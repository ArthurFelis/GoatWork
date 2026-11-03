using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Tela "Abrir disputa" (cliente ou trabalhador).</summary>
public class DisputaService
{
    private readonly IAppDbContext _db;
    private readonly ContratoService _contratos;

    public DisputaService(IAppDbContext db, ContratoService contratos)
    {
        _db = db;
        _contratos = contratos;
    }

    public async Task<DisputaContextoDto> ObterAsync(UsuarioLogado u, int idContrato)
    {
        var c = await _contratos.CarregarAsync(u, idContrato);
        var disputa = await UltimaAsync(c.IdContrato);
        return Contexto(c, u, disputa);
    }

    public async Task<DisputaContextoDto> AbrirAsync(UsuarioLogado u, int idContrato, AbrirDisputaRequest req)
    {
        var c = await _contratos.CarregarAsync(u, idContrato);
        if (ContratoService.Papel(c, u.Id) is null) throw new AcessoNegadoException("Só as partes do contrato podem abrir disputa.");
        Validacao.Exigir(c.Status == StatusContrato.ATIVO, "Só é possível abrir disputa de um contrato ativo.");
        Validacao.Exigir(c.Pagamento?.Status == StatusPagamento.EM_CUSTODIA, "A disputa só pode ser aberta com o pagamento em custódia.");
        Validacao.Exigir(req.Entendo, "Confirme que entende que o pagamento permanece em custódia.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Motivo) && req.Motivo.Trim().Length >= 20,
            "Explique o que aconteceu (mínimo de 20 caracteres).");
        Validacao.Exigir(c.Disputas.All(d => d.Status is StatusDisputa.RESOLVIDA or StatusDisputa.ARQUIVADA),
            "Já existe uma disputa aberta para este contrato.");

        var disputa = new Disputa
        {
            IdContrato = c.IdContrato,
            IdAbertor = u.Id,
            Motivo = req.Motivo.Trim(),
            Status = StatusDisputa.ABERTA,
            DataAbertura = DateTime.Now
        };
        _db.Disputas.Add(disputa);
        c.Status = StatusContrato.EM_DISPUTA;
        await _db.SaveChangesAsync();

        return Contexto(c, u, await UltimaAsync(c.IdContrato));
    }

    private Task<Disputa?> UltimaAsync(int idContrato) =>
        _db.Disputas.Include(d => d.Abertor)
            .Where(d => d.IdContrato == idContrato)
            .OrderByDescending(d => d.DataAbertura).ThenByDescending(d => d.IdDisputa)
            .FirstOrDefaultAsync();

    public static string Numero(int id) => $"#{id:D4}";

    public static DisputaDto ParaDto(Disputa d) => new(
        d.IdDisputa,
        Numero(d.IdDisputa),
        d.Motivo,
        d.Status.ToString(),
        d.Decisao,
        d.DataAbertura,
        d.DataResolucao,
        d.Abertor.Nome,
        d.Abertor.Tipo.ToString());

    private static DisputaContextoDto Contexto(Contrato c, UsuarioLogado u, Disputa? d) => new(
        c.IdContrato,
        c.Numero,
        ContratoService.NomeTrabalhador(c),
        ContratoService.NomeCliente(c),
        u.EhAdmin ? "ADMIN" : ContratoService.Papel(c, u.Id)!,
        c.ValorAcordado,
        c.Status.ToString(),
        c.Pagamento?.Status.ToString() ?? StatusPagamento.PENDENTE.ToString(),
        d is null ? null : ParaDto(d));
}
