using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Avaliação mútua (regra 7: uma por avaliador por contrato concluído).</summary>
public class AvaliacaoService
{
    private readonly IAppDbContext _db;
    private readonly ContratoService _contratos;

    public AvaliacaoService(IAppDbContext db, ContratoService contratos)
    {
        _db = db;
        _contratos = contratos;
    }

    public async Task<AvaliacaoContextoDto> ObterAsync(UsuarioLogado u, int idContrato) =>
        Contexto(await _contratos.CarregarAsync(u, idContrato), u);

    public async Task<AvaliacaoContextoDto> AvaliarAsync(UsuarioLogado u, int idContrato, AvaliarRequest req)
    {
        var c = await _contratos.CarregarAsync(u, idContrato);
        var papel = ContratoService.Papel(c, u.Id) ?? throw new AcessoNegadoException("Só as partes do contrato podem avaliar.");
        Validacao.Exigir(c.Status == StatusContrato.CONCLUIDO, "Só é possível avaliar contratos concluídos.");
        Validacao.Exigir(req.Nota is >= 1 and <= 5, "Escolha uma nota de 1 a 5.");
        Validacao.Exigir((req.Comentario ?? "").Length <= 1000, "O comentário pode ter no máximo 1000 caracteres.");
        Validacao.Exigir(c.Avaliacoes.All(a => a.IdAvaliador != u.Id), "Você já avaliou este contrato.");

        var idAvaliado = papel == "CLIENTE" ? ContratoService.IdTrabalhador(c) : ContratoService.IdCliente(c);
        var avaliacao = new Avaliacao
        {
            IdContrato = c.IdContrato,
            IdAvaliador = u.Id,
            IdAvaliado = idAvaliado,
            Nota = (byte)req.Nota,
            Comentario = string.IsNullOrWhiteSpace(req.Comentario) ? null : req.Comentario.Trim(),
            Data = DateTime.Now
        };
        _db.Avaliacoes.Add(avaliacao);
        c.Avaliacoes.Add(avaliacao);

        // Atualiza a nota média do profissional (média ponderada pelo total de avaliações)
        if (papel == "CLIENTE")
        {
            var t = c.Chat.Convite.Trabalhador;
            t.NotaMedia = Math.Round((t.NotaMedia * t.TotalAvaliacoes + req.Nota) / (t.TotalAvaliacoes + 1), 2);
            t.TotalAvaliacoes += 1;
        }

        await _db.SaveChangesAsync();
        return Contexto(c, u);
    }

    private static AvaliacaoContextoDto Contexto(Contrato c, UsuarioLogado u)
    {
        var papel = u.EhAdmin ? "ADMIN" : ContratoService.Papel(c, u.Id)!;
        var avaliaTrabalhador = papel != "TRABALHADOR";
        var trab = c.Chat.Convite.Trabalhador;
        var avaliado = avaliaTrabalhador ? ContratoService.NomeTrabalhador(c) : ContratoService.NomeCliente(c);
        var subtitulo = avaliaTrabalhador ? Texto.SubtituloTrabalhador(trab.Profissao, trab.Cidade) : "Cliente";
        var minha = c.Avaliacoes.FirstOrDefault(a => a.IdAvaliador == u.Id);

        return new AvaliacaoContextoDto(
            c.IdContrato,
            c.Numero,
            c.Status.ToString(),
            papel,
            avaliado,
            Texto.Iniciais(avaliado),
            subtitulo,
            ContratoService.Resumo(c),
            c.ValorAcordado,
            c.Pagamento?.Status.ToString() ?? StatusPagamento.PENDENTE.ToString(),
            c.Pagamento?.DataLiberacao,
            minha is not null,
            minha?.Nota);
    }
}
