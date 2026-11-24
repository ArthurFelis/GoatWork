using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Chat interno monitorado entre cliente e trabalhador (CHAT + MENSAGEM).</summary>
public class ChatService
{
    private readonly IAppDbContext _db;
    private readonly FiltroContatoExterno _filtro;

    public ChatService(IAppDbContext db, FiltroContatoExterno filtro)
    {
        _db = db;
        _filtro = filtro;
    }

    private IQueryable<Chat> ComDetalhes() =>
        _db.Chats
            .Include(c => c.Contrato)
            .Include(c => c.Mensagens)
            .Include(c => c.Convite).ThenInclude(cv => cv.Trabalhador).ThenInclude(t => t.Usuario)
            .Include(c => c.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Cliente).ThenInclude(cl => cl.Usuario)
            .Include(c => c.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Briefing);

    /// <summary>Lista "Conversas" (barra lateral da tela Mensagens).</summary>
    public async Task<List<ConversaResumoDto>> ListarAsync(UsuarioLogado usuario)
    {
        var query = ComDetalhes();
        query = usuario.EhTrabalhador
            ? query.Where(c => c.Convite.IdTrabalhador == usuario.Id)
            : query.Where(c => c.Convite.Pedido.IdCliente == usuario.Id);

        var chats = await query.ToListAsync();
        return chats
            .Select(c =>
            {
                var ultima = c.Mensagens.Count == 0 ? c.DataAbertura : c.Mensagens.Max(m => m.DataHora);
                var etapa = c.Contrato is { AceiteClienteEm: not null } ? $"Contrato {c.Contrato.Numero}"
                    : c.Contrato is not null ? "Orçamento enviado"
                    : c.Status == StatusChat.ABERTO ? "Em negociação" : "Encerrado";
                return new ConversaResumoDto(c.IdChat, OutraParte(c, usuario), $"{etapa} · Pedido {Texto.NumeroPedido(c.Convite.IdPedido)}", ultima);
            })
            .OrderByDescending(c => c.UltimaAtividade)
            .ToList();
    }

    public async Task<ChatDto> ObterAsync(UsuarioLogado usuario, int idChat)
    {
        var chat = await CarregarAsync(usuario, idChat);
        return ParaDto(chat, usuario);
    }

    public async Task<ChatDto> EnviarAsync(UsuarioLogado usuario, int idChat, EnviarMensagemRequest req)
    {
        var chat = await CarregarAsync(usuario, idChat);
        Validacao.Exigir(!usuario.EhAdmin, "Administradores não enviam mensagens no chat.");
        Validacao.Exigir(chat.Status == StatusChat.ABERTO, "Esta conversa está encerrada.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Conteudo), "Escreva sua mensagem.");
        Validacao.Exigir(req.Conteudo.Length <= 2000, "Mensagem muito longa.");

        chat.Mensagens.Add(new Mensagem
        {
            IdRemetente = usuario.Id,
            Conteudo = req.Conteudo.Trim(),
            Bloqueada = _filtro.ContemContatoExterno(req.Conteudo),
            DataHora = DateTime.Now
        });
        await _db.SaveChangesAsync();
        return ParaDto(chat, usuario);
    }

    internal async Task<Chat> CarregarAsync(UsuarioLogado usuario, int idChat)
    {
        var chat = await ComDetalhes().FirstOrDefaultAsync(c => c.IdChat == idChat)
                   ?? throw new NaoEncontradoException("Conversa não encontrada.");
        var participa = usuario.EhAdmin
                        || chat.Convite.IdTrabalhador == usuario.Id
                        || chat.Convite.Pedido.IdCliente == usuario.Id;
        if (!participa) throw new AcessoNegadoException();
        return chat;
    }

    private static string NomeCliente(Chat c) => c.Convite.Pedido.Cliente.NomeExibicao ?? c.Convite.Pedido.Cliente.Usuario.Nome;

    private static string OutraParte(Chat c, UsuarioLogado u) =>
        u.EhTrabalhador ? NomeCliente(c) : c.Convite.Trabalhador.Usuario.Nome;

    private static ChatDto ParaDto(Chat c, UsuarioLogado u)
    {
        var numeroPedido = Texto.NumeroPedido(c.Convite.IdPedido);
        var subtitulo = u.EhTrabalhador
            ? $"Cliente · Pedido {numeroPedido}"
            : $"{c.Convite.Trabalhador.Profissao ?? "Profissional"} · Pedido {numeroPedido}";

        var visiveis = c.Mensagens
            .OrderBy(m => m.IdMensagem)
            // mensagens bloqueadas só aparecem (mascaradas) para quem enviou
            .Where(m => !m.Bloqueada || m.IdRemetente == u.Id)
            .Select(m =>
            {
                var minha = m.IdRemetente == u.Id;
                var nome = m.IdRemetente == c.Convite.IdTrabalhador ? c.Convite.Trabalhador.Usuario.Nome : NomeCliente(c);
                return new MensagemDto(m.IdMensagem, nome, minha, m.Bloqueada ? FiltroContatoExterno.Mascarar(m.Conteudo) : m.Conteudo, m.Bloqueada, m.DataHora);
            })
            .ToList();

        var pedido = c.Convite.Pedido;
        var briefing = pedido.Briefing;
        var fixado = new FixadoChatDto(
            briefing?.Resumo ?? Texto.TituloPedido(pedido.DescricaoInicial),
            Texto.Local(briefing?.Requisitos),
            briefing?.PrazoDesejado,
            briefing?.OrcamentoEstimado);

        PropostaChatDto? proposta = c.Contrato is { } ct
            ? new PropostaChatDto(ct.IdContrato, ct.Numero, ct.ValorAcordado, ct.PrazoEntrega, ct.HoraServico, ct.LocalServico,
                ct.Status.ToString(), ct.AceiteClienteEm is not null, ct.AceiteTrabalhadorEm is not null)
            : null;

        // Pré-preenche o formulário "Enviar orçamento" com o que já foi conversado
        SugestaoOrcamentoDto? sugestao = null;
        if (u.EhTrabalhador && c.Status == StatusChat.ABERTO && proposta is not { AssinadoCliente: true })
        {
            var textos = c.Mensagens.Where(m => !m.Bloqueada).OrderBy(m => m.IdMensagem).Select(m => m.Conteudo).ToList();
            var sug = DetectorAcordo.Sugerir(textos, DateOnly.FromDateTime(DateTime.Today), briefing?.PrazoDesejado, briefing?.OrcamentoEstimado);
            sugestao = proposta is not null
                ? new SugestaoOrcamentoDto(proposta.Valor, proposta.Data, proposta.Hora, proposta.Local)
                : new SugestaoOrcamentoDto(sug.Valor, sug.Data, sug.Hora, fixado.Local);
        }

        return new ChatDto(
            c.IdChat,
            c.Convite.IdPedido,
            numeroPedido,
            OutraParte(c, u),
            subtitulo,
            c.Status.ToString(),
            u.EhAdmin ? "ADMIN" : u.EhTrabalhador ? "TRABALHADOR" : "CLIENTE",
            visiveis,
            fixado,
            proposta,
            sugestao,
            pedido.Status != StatusPedido.PUBLICADO && c.Contrato is null);
    }
}
