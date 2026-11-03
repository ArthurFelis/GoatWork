using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Telas "Entrega do serviço" (trabalhador) e "Aprovar entrega" (cliente).</summary>
public class EntregaService
{
    private readonly IAppDbContext _db;
    private readonly ContratoService _contratos;
    private readonly PagamentoService _pagamentos;

    public EntregaService(IAppDbContext db, ContratoService contratos, PagamentoService pagamentos)
    {
        _db = db;
        _contratos = contratos;
        _pagamentos = pagamentos;
    }

    public async Task<EntregasContratoDto> ObterAsync(UsuarioLogado u, int idContrato) =>
        ParaDto(await _contratos.CarregarAsync(u, idContrato), u);

    /// <summary>"Enviar entrega" (regra 8: após rejeição, pode enviar outra no mesmo contrato).</summary>
    public async Task<EntregasContratoDto> EnviarAsync(UsuarioLogado u, int idContrato, EnviarEntregaRequest req)
    {
        var c = await _contratos.CarregarAsync(u, idContrato);
        if (ContratoService.Papel(c, u.Id) != "TRABALHADOR") throw new AcessoNegadoException("Só o trabalhador registra entregas.");
        Validacao.Exigir(c.Status == StatusContrato.ATIVO, "O contrato não está ativo.");
        Validacao.Exigir(c.Pagamento?.Status == StatusPagamento.EM_CUSTODIA, "Aguarde o pagamento do cliente ficar em custódia.");
        Validacao.Exigir(c.Entregas.All(e => e.Status != StatusEntrega.PENDENTE), "Já existe uma entrega aguardando a aprovação do cliente.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Descricao) && req.Descricao.Trim().Length >= 10, "Descreva o que foi entregue.");

        var entrega = new Entrega { IdContrato = c.IdContrato, Descricao = req.Descricao.Trim(), DataEntrega = DateTime.Now, Status = StatusEntrega.PENDENTE };
        _db.Entregas.Add(entrega);
        c.Entregas.Add(entrega);
        await _db.SaveChangesAsync();
        return ParaDto(c, u);
    }

    /// <summary>"Aprovar e liberar pagamento": libera o valor líquido e conclui o contrato.</summary>
    public async Task<EntregasContratoDto> AprovarAsync(UsuarioLogado u, int idEntrega)
    {
        var (c, entrega) = await CarregarParaClienteAsync(u, idEntrega);

        entrega.Status = StatusEntrega.APROVADA;
        await _pagamentos.LiberarAsync(c.Pagamento!);
        Concluir(c);
        await _db.SaveChangesAsync();
        return ParaDto(c, u);
    }

    public async Task<EntregasContratoDto> RejeitarAsync(UsuarioLogado u, int idEntrega)
    {
        var (c, entrega) = await CarregarParaClienteAsync(u, idEntrega);
        entrega.Status = StatusEntrega.REJEITADA;
        await _db.SaveChangesAsync();
        return ParaDto(c, u);
    }

    /// <summary>Encerra o contrato com sucesso (usado também pela decisão de disputa).</summary>
    internal static void Concluir(Contrato c)
    {
        c.Status = StatusContrato.CONCLUIDO;
        c.Chat.Status = StatusChat.FECHADO;
        c.Chat.Convite.Pedido.Status = StatusPedido.CONCLUIDO;
        c.Chat.Convite.Trabalhador.ServicosConcluidos += 1;
    }

    private async Task<(Contrato, Entrega)> CarregarParaClienteAsync(UsuarioLogado u, int idEntrega)
    {
        var idContrato = await _db.Entregas.Where(e => e.IdEntrega == idEntrega).Select(e => (int?)e.IdContrato).FirstOrDefaultAsync()
                         ?? throw new NaoEncontradoException("Entrega não encontrada.");
        var c = await _contratos.CarregarAsync(u, idContrato);
        if (ContratoService.Papel(c, u.Id) != "CLIENTE") throw new AcessoNegadoException("Só o cliente aprova ou rejeita entregas.");
        var entrega = c.Entregas.First(e => e.IdEntrega == idEntrega);
        Validacao.Exigir(entrega.Status == StatusEntrega.PENDENTE, "Esta entrega já foi avaliada.");
        Validacao.Exigir(c.Status == StatusContrato.ATIVO, "O contrato não está ativo.");
        Validacao.Exigir(c.Pagamento?.Status == StatusPagamento.EM_CUSTODIA, "O pagamento não está em custódia.");
        return (c, entrega);
    }

    private static EntregasContratoDto ParaDto(Contrato c, UsuarioLogado u)
    {
        var (taxa, liquido) = Etapas.Valores(c.ValorAcordado, c.TaxaPlataformaPerc);
        var briefing = c.Chat.Convite.Pedido.Briefing;
        var requisitos = new List<string> { ContratoService.Resumo(c).TrimEnd('.') };
        requisitos.AddRange(Texto.Linhas(briefing?.Requisitos)
            .Where(r => !r.StartsWith("Local", StringComparison.OrdinalIgnoreCase) && !r.StartsWith("Garantir", StringComparison.OrdinalIgnoreCase)));

        var numero = 0;
        var entregas = c.Entregas.OrderBy(e => e.DataEntrega).ThenBy(e => e.IdEntrega)
            .Select(e => new EntregaDto(e.IdEntrega, ++numero, e.Descricao, e.DataEntrega, e.Status.ToString()))
            .ToList();

        return new EntregasContratoDto(
            c.IdContrato,
            c.Numero,
            c.Status.ToString(),
            u.EhAdmin ? "ADMIN" : ContratoService.Papel(c, u.Id)!,
            ContratoService.NomeCliente(c),
            ContratoService.NomeTrabalhador(c),
            Texto.Iniciais(ContratoService.NomeTrabalhador(c)),
            ContratoService.Resumo(c),
            requisitos,
            c.PrazoEntrega,
            c.ValorAcordado,
            taxa,
            liquido,
            c.Pagamento?.Status.ToString() ?? StatusPagamento.PENDENTE.ToString(),
            entregas);
    }
}
