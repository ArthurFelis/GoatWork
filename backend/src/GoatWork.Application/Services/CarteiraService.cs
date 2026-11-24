using System.Text.RegularExpressions;
using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>
/// Tela "Minha carteira" do trabalhador.
/// Saldo disponível = valores líquidos LIBERADOS dos contratos − saques (pagos ou em processamento).
/// O que ainda está EM_CUSTODIA aparece como "a receber", mas não pode ser sacado.
/// </summary>
public class CarteiraService
{
    private readonly IAppDbContext _db;
    private readonly IPagamentoGateway _gateway;
    private readonly PlataformaOptions _plataforma;

    public CarteiraService(IAppDbContext db, IPagamentoGateway gateway, PlataformaOptions plataforma)
    {
        _db = db;
        _gateway = gateway;
        _plataforma = plataforma;
    }

    public async Task<CarteiraDto> ObterAsync(int idTrabalhador)
    {
        var trabalhador = await _db.Trabalhadores.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.IdUsuario == idTrabalhador)
                          ?? throw new NaoEncontradoException("Trabalhador não encontrado.");

        var pagamentos = await _db.Pagamentos
            .Include(p => p.Contrato).ThenInclude(c => c.Chat).ThenInclude(ch => ch.Convite).ThenInclude(cv => cv.Pedido)
            .Where(p => p.Contrato.Chat.Convite.IdTrabalhador == idTrabalhador
                        && (p.Status == StatusPagamento.LIBERADO || p.Status == StatusPagamento.EM_CUSTODIA))
            .ToListAsync();
        var saques = await _db.Saques.Where(s => s.IdTrabalhador == idTrabalhador).ToListAsync();

        var recebido = pagamentos.Where(p => p.Status == StatusPagamento.LIBERADO).Sum(p => p.ValorLiquido);
        var custodia = pagamentos.Where(p => p.Status == StatusPagamento.EM_CUSTODIA).Sum(p => p.ValorLiquido);
        var sacado = saques.Where(s => s.Status != StatusSaque.FALHOU).Sum(s => s.Valor);

        var movimentos = new List<MovimentacaoDto>();
        foreach (var p in pagamentos)
        {
            var servico = Texto.TituloPedido(p.Contrato.Chat.Convite.Pedido.DescricaoInicial);
            movimentos.Add(p.Status == StatusPagamento.LIBERADO
                ? new MovimentacaoDto("ENTRADA", $"Contrato {p.Contrato.Numero} · {servico}", p.ValorLiquido,
                    p.DataLiberacao ?? p.DataPagamento ?? DateTime.Now, "LIBERADO")
                : new MovimentacaoDto("A_RECEBER", $"Contrato {p.Contrato.Numero} · {servico}", p.ValorLiquido,
                    p.DataPagamento ?? DateTime.Now, "EM_CUSTODIA"));
        }
        movimentos.AddRange(saques.Select(s => new MovimentacaoDto("SAQUE",
            $"Saque via Pix · {NomeTipo(s.TipoChave)} {MascararChave(s.TipoChave, s.ChavePix)}",
            -s.Valor, s.DataSolicitacao, s.Status.ToString())));

        var juridica = trabalhador.TipoPessoa == TipoPessoa.JURIDICA;
        return new CarteiraDto(
            recebido - sacado,
            custodia,
            recebido,
            sacado,
            _plataforma.SaqueMinimo,
            juridica ? "CNPJ" : "CPF",
            trabalhador.Usuario.Cpf,
            movimentos.OrderByDescending(m => m.Data).ToList());
    }

    /// <summary>"Retirar": transfere o valor para a chave Pix informada.</summary>
    public async Task<CarteiraDto> SacarAsync(int idTrabalhador, SacarRequest req)
    {
        if (!Enum.TryParse<TipoChavePix>(req.TipoChave, true, out var tipo))
            throw new RegraNegocioException("Escolha o tipo da chave Pix.");
        var chave = NormalizarChave(tipo, req.Chave);
        var valor = Math.Round(req.Valor, 2);

        var carteira = await ObterAsync(idTrabalhador);
        Validacao.Exigir(valor >= _plataforma.SaqueMinimo, $"O valor mínimo para saque é {Texto.Moeda(_plataforma.SaqueMinimo)}.");
        Validacao.Exigir(valor <= carteira.Disponivel, $"Saldo insuficiente. Disponível: {Texto.Moeda(carteira.Disponivel)}.");

        var saque = new Saque
        {
            IdTrabalhador = idTrabalhador,
            Valor = valor,
            TipoChave = tipo,
            ChavePix = chave,
            Status = StatusSaque.SOLICITADO,
            DataSolicitacao = DateTime.Now
        };
        _db.Saques.Add(saque);
        await _db.SaveChangesAsync(); // reserva o valor antes de chamar o gateway

        var resultado = await _gateway.TransferirPixAsync($"SAQUE-{saque.IdSaque}", valor, tipo.ToString(), chave);
        saque.Status = resultado.Aprovado ? StatusSaque.PAGO : StatusSaque.FALHOU;
        saque.IdTransacaoGateway = resultado.IdTransacao;
        saque.DataConclusao = DateTime.Now;
        await _db.SaveChangesAsync();

        if (!resultado.Aprovado)
            throw new RegraNegocioException(resultado.Mensagem ?? "Não foi possível concluir o saque. O valor voltou para o seu saldo.");
        return await ObterAsync(idTrabalhador);
    }

    private static string NormalizarChave(TipoChavePix tipo, string? chave)
    {
        var c = (chave ?? string.Empty).Trim();
        switch (tipo)
        {
            case TipoChavePix.CPF:
                Validacao.Exigir(Validacao.CpfValido(c), "Chave Pix inválida: informe um CPF válido.");
                return Validacao.SoDigitos(c);
            case TipoChavePix.CNPJ:
                Validacao.Exigir(Validacao.CnpjValido(c), "Chave Pix inválida: informe um CNPJ válido.");
                return Validacao.SoDigitos(c);
            case TipoChavePix.EMAIL:
                Validacao.Exigir(Validacao.EmailValido(c) && c.Length <= 77, "Chave Pix inválida: informe um e-mail válido.");
                return c.ToLowerInvariant();
            case TipoChavePix.TELEFONE:
                var d = Validacao.SoDigitos(c);
                if (d.Length == 13 && d.StartsWith("55")) d = d[2..];
                Validacao.Exigir(d.Length is 10 or 11, "Chave Pix inválida: informe o celular com DDD.");
                return "+55" + d;
            default:
                Validacao.Exigir(Regex.IsMatch(c, "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"),
                    "Chave Pix inválida: a chave aleatória tem 36 caracteres (ex.: 123e4567-e89b-12d3-a456-426614174000).");
                return c.ToLowerInvariant();
        }
    }

    private static string NomeTipo(TipoChavePix t) => t switch
    {
        TipoChavePix.EMAIL => "e-mail",
        TipoChavePix.TELEFONE => "celular",
        TipoChavePix.ALEATORIA => "chave aleatória",
        _ => t.ToString()
    };

    /// <summary>Mostra só parte da chave no histórico (ex.: ***.456.789-**, ca***@gmail.com).</summary>
    public static string MascararChave(TipoChavePix tipo, string chave) => tipo switch
    {
        TipoChavePix.CPF when chave.Length == 11 => $"***.{chave[3..6]}.{chave[6..9]}-**",
        TipoChavePix.CNPJ when chave.Length == 14 => $"**.{chave[2..5]}.{chave[5..8]}/****-**",
        TipoChavePix.EMAIL when chave.IndexOf('@') > 2 => chave[..2] + "***" + chave[chave.IndexOf('@')..],
        TipoChavePix.TELEFONE when chave.Length >= 8 => "(**) *****-" + chave[^4..],
        TipoChavePix.ALEATORIA when chave.Length > 8 => chave[..8] + "…",
        _ => "***"
    };
}
