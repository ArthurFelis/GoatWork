using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Tela "Convites recebidos" do trabalhador.</summary>
public class ConviteService
{
    private readonly IAppDbContext _db;

    public ConviteService(IAppDbContext db) => _db = db;

    public async Task<List<ConviteDto>> ListarAsync(int idTrabalhador, string? status)
    {
        var query = _db.Convites
            .Include(c => c.Chat)
            .Include(c => c.Pedido).ThenInclude(p => p.Categoria)
            .Include(c => c.Pedido).ThenInclude(p => p.Briefing)
            .Include(c => c.Pedido).ThenInclude(p => p.Cliente).ThenInclude(cl => cl.Usuario)
            .Where(c => c.IdTrabalhador == idTrabalhador && c.Pedido.Briefing != null && c.Pedido.Briefing.ConfirmadoCliente);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusConvite>(status, true, out var s))
            query = query.Where(c => c.Status == s);

        var lista = await query.OrderByDescending(c => c.DataEnvio).ToListAsync();
        return lista.Select(ParaDto).ToList();
    }

    /// <summary>"Aceitar e abrir chat": regra 3 — o chat só existe se o convite for aceito.</summary>
    public async Task<int> AceitarAsync(int idTrabalhador, int idConvite)
    {
        var convite = await Carregar(idTrabalhador, idConvite);
        if (convite.Status == StatusConvite.ACEITO && convite.Chat is not null) return convite.Chat.IdChat;
        Validacao.Exigir(convite.Status == StatusConvite.ENVIADO, "Este convite já foi respondido.");
        Validacao.Exigir(convite.Pedido.Status == StatusPedido.PUBLICADO, "Este pedido não está mais disponível.");

        convite.Status = StatusConvite.ACEITO;
        convite.DataResposta = DateTime.Now;
        convite.Chat = new Chat { Status = StatusChat.ABERTO, DataAbertura = DateTime.Now };
        await _db.SaveChangesAsync();
        return convite.Chat.IdChat;
    }

    public async Task RecusarAsync(int idTrabalhador, int idConvite)
    {
        var convite = await Carregar(idTrabalhador, idConvite);
        Validacao.Exigir(convite.Status == StatusConvite.ENVIADO, "Este convite já foi respondido.");
        convite.Status = StatusConvite.RECUSADO;
        convite.DataResposta = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    private async Task<Convite> Carregar(int idTrabalhador, int idConvite)
    {
        var convite = await _db.Convites
                          .Include(c => c.Chat)
                          .Include(c => c.Pedido)
                          .FirstOrDefaultAsync(c => c.IdConvite == idConvite)
                      ?? throw new NaoEncontradoException("Convite não encontrado.");
        if (convite.IdTrabalhador != idTrabalhador) throw new AcessoNegadoException();
        return convite;
    }

    private static ConviteDto ParaDto(Convite c)
    {
        var p = c.Pedido;
        var b = p.Briefing!;
        var requisitos = Texto.Linhas(b.Requisitos).Where(r => !r.StartsWith("Local", StringComparison.OrdinalIgnoreCase));
        var resumo = string.Join(" ", new[] { b.Resumo }.Concat(requisitos.Take(1).Select(r => r.TrimEnd('.') + ".")));
        var cliente = p.Cliente.NomeExibicao ?? p.Cliente.Usuario.Nome;
        return new ConviteDto(
            c.IdConvite,
            c.Status.ToString(),
            p.IdPedido,
            Texto.TituloPedido(p.DescricaoInicial),
            resumo,
            p.Categoria.Nome,
            cliente,
            Texto.Iniciais(cliente),
            c.DataEnvio,
            Texto.LocalCurto(b.Requisitos),
            b.PrazoDesejado,
            b.OrcamentoEstimado,
            c.Chat?.IdChat);
    }
}
