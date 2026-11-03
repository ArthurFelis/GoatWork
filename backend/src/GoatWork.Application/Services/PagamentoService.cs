using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;

namespace GoatWork.Application.Services;

/// <summary>Pagamento em custódia (regra 5) e cálculo do repasse (regra 6).</summary>
public class PagamentoService
{
    private readonly IAppDbContext _db;
    private readonly IPagamentoGateway _gateway;
    private readonly ContratoService _contratos;

    public PagamentoService(IAppDbContext db, IPagamentoGateway gateway, ContratoService contratos)
    {
        _db = db;
        _gateway = gateway;
        _contratos = contratos;
    }

    public async Task<PagamentoResumoDto> ObterAsync(UsuarioLogado u, int idContrato) =>
        Resumo(await _contratos.CarregarAsync(u, idContrato));

    /// <summary>Botão "Pagar R$ ...": cobra pelo gateway e deixa o valor EM_CUSTODIA.</summary>
    public async Task<PagamentoResumoDto> PagarAsync(UsuarioLogado u, int idContrato, PagarRequest req)
    {
        var c = await _contratos.CarregarAsync(u, idContrato);
        if (ContratoService.Papel(c, u.Id) != "CLIENTE") throw new AcessoNegadoException("Só o cliente pode pagar o contrato.");
        Validacao.Exigir(c.Status == StatusContrato.ATIVO, "O contrato precisa estar assinado pelas duas partes antes do pagamento.");
        Validacao.Exigir(c.Pagamento is null || c.Pagamento.Status is StatusPagamento.PENDENTE or StatusPagamento.FALHOU,
            "Este contrato já foi pago.");

        if (!Enum.TryParse<MetodoPagamento>(req.Metodo, true, out var metodo))
            throw new RegraNegocioException("Escolha Pix ou cartão.");

        if (metodo == MetodoPagamento.CARTAO)
        {
            var numero = Validacao.SoDigitos(req.NumeroCartao);
            Validacao.Exigir(numero.Length is >= 13 and <= 19, "Número do cartão inválido.");
            Validacao.Exigir(!string.IsNullOrWhiteSpace(req.NomeCartao), "Informe o nome impresso no cartão.");
            Validacao.Exigir(System.Text.RegularExpressions.Regex.IsMatch(req.Validade ?? "", @"^(0[1-9]|1[0-2])/\d{2}$"), "Validade inválida (use MM/AA).");
            Validacao.Exigir(Validacao.SoDigitos(req.Cvv).Length is 3 or 4, "CVV inválido.");
        }

        var (taxa, liquido) = Etapas.Valores(c.ValorAcordado, c.TaxaPlataformaPerc);
        var pagamento = c.Pagamento ?? new Pagamento { IdContrato = c.IdContrato };
        pagamento.Metodo = metodo;
        pagamento.ValorTotal = c.ValorAcordado;
        pagamento.ValorTaxa = taxa;
        pagamento.ValorLiquido = liquido;
        pagamento.Status = StatusPagamento.PENDENTE;
        if (c.Pagamento is null)
        {
            _db.Pagamentos.Add(pagamento);
            c.Pagamento = pagamento;
        }

        // Os dados do cartão vão apenas para o gateway; nada é gravado no banco.
        var resultado = await _gateway.CobrarAsync(new CobrancaGateway(
            c.Numero, metodo.ToString(), c.ValorAcordado, req.NumeroCartao, req.NomeCartao, req.Validade, req.Cvv));

        if (resultado.Aprovado)
        {
            pagamento.Status = StatusPagamento.EM_CUSTODIA;
            pagamento.IdTransacaoGateway = resultado.IdTransacao;
            pagamento.DataPagamento = DateTime.Now;
        }
        else
        {
            pagamento.Status = StatusPagamento.FALHOU;
        }
        await _db.SaveChangesAsync();

        if (!resultado.Aprovado)
            throw new RegraNegocioException(resultado.Mensagem ?? "Pagamento recusado. Tente outro método.");
        return Resumo(c);
    }

    /// <summary>Entrega aprovada ou disputa decidida a favor do trabalhador.</summary>
    internal async Task LiberarAsync(Pagamento p)
    {
        Validacao.Exigir(p.Status == StatusPagamento.EM_CUSTODIA, "O pagamento não está em custódia.");
        await _gateway.LiberarAsync(p.IdTransacaoGateway ?? string.Empty, p.ValorLiquido);
        p.Status = StatusPagamento.LIBERADO;
        p.DataLiberacao = DateTime.Now;
    }

    /// <summary>Disputa decidida a favor do cliente.</summary>
    internal async Task EstornarAsync(Pagamento p)
    {
        Validacao.Exigir(p.Status == StatusPagamento.EM_CUSTODIA, "O pagamento não está em custódia.");
        await _gateway.EstornarAsync(p.IdTransacaoGateway ?? string.Empty, p.ValorTotal);
        p.Status = StatusPagamento.REEMBOLSADO;
        p.DataLiberacao = DateTime.Now;
    }

    private PagamentoResumoDto Resumo(Contrato c)
    {
        var (taxa, liquido) = Etapas.Valores(c.ValorAcordado, c.TaxaPlataformaPerc);
        return new PagamentoResumoDto(
            c.IdContrato,
            c.Numero,
            c.Status.ToString(),
            c.ValorAcordado,
            c.TaxaPlataformaPerc,
            taxa,
            liquido,
            c.Pagamento?.Status.ToString() ?? StatusPagamento.PENDENTE.ToString(),
            c.Pagamento?.Metodo.ToString(),
            _gateway.GerarPixCopiaECola(c.Numero, c.ValorAcordado),
            ContratoService.Resumo(c),
            ContratoService.NomeTrabalhador(c),
            c.PrazoEntrega,
            c.Pagamento?.DataPagamento);
    }
}
