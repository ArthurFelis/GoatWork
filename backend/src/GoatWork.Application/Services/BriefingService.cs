using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Conversa com a IA de atendimento (MENSAGEM_IA) e o briefing gerado (BRIEFING).</summary>
public class BriefingService
{
    private readonly IAppDbContext _db;
    private readonly IIaAtendimento _ia;

    public BriefingService(IAppDbContext db, IIaAtendimento ia)
    {
        _db = db;
        _ia = ia;
    }

    private Task<Pedido?> CarregarAsync(int idPedido) =>
        _db.Pedidos
            .Include(p => p.Categoria)
            .Include(p => p.Briefing)
            .Include(p => p.MensagensIa)
            .Include(p => p.TrabalhadorPreferido!).ThenInclude(t => t.Usuario)
            .Include(p => p.Convites).ThenInclude(c => c.Trabalhador).ThenInclude(t => t.Usuario)
            .Include(p => p.Convites).ThenInclude(c => c.Chat!).ThenInclude(ch => ch.Contrato)
            .FirstOrDefaultAsync(p => p.IdPedido == idPedido);

    private async Task<Pedido> CarregarDoClienteAsync(int idCliente, int idPedido)
    {
        var p = await CarregarAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (p.IdCliente != idCliente) throw new AcessoNegadoException();
        return p;
    }

    /// <summary>Abre a conversa: saudação da IA + descrição do cliente + primeira resposta da IA.</summary>
    public async Task IniciarConversaAsync(int idPedido)
    {
        var pedido = await CarregarAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (pedido.MensagensIa.Count > 0) return;

        // Cada mensagem é gravada antes da próxima: o id (auto-incremento) define a ordem da conversa
        pedido.MensagensIa.Add(new MensagemIa { Remetente = RemetenteIa.IA, Conteudo = AtendimentoIaRegras.Saudacao, DataHora = DateTime.Now });
        await _db.SaveChangesAsync();
        pedido.MensagensIa.Add(new MensagemIa { Remetente = RemetenteIa.CLIENTE, Conteudo = pedido.DescricaoInicial, DataHora = DateTime.Now });
        await _db.SaveChangesAsync();
        await ResponderComIaAsync(pedido);
        await _db.SaveChangesAsync();
    }

    public async Task<ConversaIaDto> ObterConversaAsync(int idCliente, int idPedido)
    {
        var pedido = await CarregarDoClienteAsync(idCliente, idPedido);
        if (pedido.Status == StatusPedido.EM_BRIEFING && pedido.MensagensIa.Count == 0)
        {
            await IniciarConversaAsync(idPedido);
            pedido = await CarregarDoClienteAsync(idCliente, idPedido);
        }
        return Conversa(pedido);
    }

    public async Task<ConversaIaDto> EnviarMensagemAsync(int idCliente, int idPedido, EnviarMensagemRequest req)
    {
        var pedido = await CarregarDoClienteAsync(idCliente, idPedido);
        Validacao.Exigir(pedido.Status == StatusPedido.EM_BRIEFING, "Este pedido já foi enviado aos profissionais.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Conteudo), "Escreva sua mensagem.");
        Validacao.Exigir(req.Conteudo.Length <= 2000, "Mensagem muito longa.");

        pedido.MensagensIa.Add(new MensagemIa { Remetente = RemetenteIa.CLIENTE, Conteudo = req.Conteudo.Trim(), DataHora = DateTime.Now });
        await _db.SaveChangesAsync();
        await ResponderComIaAsync(pedido);
        await _db.SaveChangesAsync();
        return Conversa(pedido);
    }

    private async Task ResponderComIaAsync(Pedido pedido)
    {
        var historico = pedido.MensagensIa
            .OrderBy(m => m.IdMsgIa)
            .Select(m => new MensagemContextoIa(m.Remetente, m.Conteudo))
            .ToList();
        var contexto = new ContextoIa(pedido.Categoria.Nome, pedido.DescricaoInicial, historico, DateOnly.FromDateTime(DateTime.Today));

        var resposta = await _ia.ProximaRespostaAsync(contexto);

        pedido.MensagensIa.Add(new MensagemIa { Remetente = RemetenteIa.IA, Conteudo = resposta.Mensagem, DataHora = DateTime.Now });

        if (resposta.Briefing is { } b)
        {
            pedido.Briefing ??= new Briefing { IdPedido = pedido.IdPedido };
            pedido.Briefing.Resumo = b.Resumo;
            pedido.Briefing.Requisitos = string.Join("\n", b.Requisitos);
            pedido.Briefing.PrazoDesejado = b.PrazoDesejado ?? pedido.Briefing.PrazoDesejado;
            pedido.Briefing.OrcamentoEstimado = b.OrcamentoEstimado ?? pedido.Briefing.OrcamentoEstimado;
            pedido.Briefing.ConfirmadoCliente = false;
            pedido.Briefing.DataConfirmacao = null;
        }
    }

    /// <summary>Tela "Revise seu briefing" (cliente) e detalhes do pedido no convite (trabalhador).</summary>
    public async Task<BriefingDto> ObterBriefingAsync(int idUsuario, TipoUsuario tipo, int idPedido)
    {
        var pedido = await CarregarAsync(idPedido) ?? throw new NaoEncontradoException("Pedido não encontrado.");
        var podeVer = tipo == TipoUsuario.ADMIN
                      || pedido.IdCliente == idUsuario
                      || pedido.Convites.Any(c => c.IdTrabalhador == idUsuario);
        if (!podeVer) throw new AcessoNegadoException();
        if (pedido.Briefing is null) throw new NaoEncontradoException("O briefing ainda não foi gerado. Continue a conversa com a IA.");
        return ParaDto(pedido);
    }

    /// <summary>
    /// "Confirmar e enviar pedido": grava os ajustes, marca o briefing como confirmado (regra 2)
    /// e envia convites aos profissionais compatíveis com a categoria.
    /// </summary>
    public async Task<BriefingDto> ConfirmarAsync(int idCliente, int idPedido, ConfirmarBriefingRequest req)
    {
        var pedido = await CarregarDoClienteAsync(idCliente, idPedido);
        Validacao.Exigir(pedido.Briefing is not null, "O briefing ainda não foi gerado.");
        Validacao.Exigir(pedido.Status == StatusPedido.EM_BRIEFING, "Este pedido já foi enviado.");
        Validacao.Exigir(req.Confirmo, "Confirme que o briefing representa o que você precisa.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Resumo), "O resumo não pode ficar vazio.");
        Validacao.Exigir(req.OrcamentoEstimado is null or >= 0m, "Orçamento inválido.");
        Validacao.Exigir(req.PrazoDesejado is null || req.PrazoDesejado >= DateOnly.FromDateTime(DateTime.Today),
            "O prazo desejado não pode estar no passado.");

        var briefing = pedido.Briefing!;
        briefing.Resumo = req.Resumo.Trim();
        briefing.Requisitos = string.Join("\n", (req.Requisitos ?? new List<string>()).Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()));
        briefing.PrazoDesejado = req.PrazoDesejado;
        briefing.OrcamentoEstimado = req.OrcamentoEstimado;
        briefing.ConfirmadoCliente = true;
        briefing.DataConfirmacao = DateTime.Now;
        pedido.Status = StatusPedido.PUBLICADO;

        // Profissionais compatíveis: o escolhido em "Contratar" ou todos da categoria
        var query = _db.Trabalhadores.Where(t => t.Usuario.Status == StatusUsuario.ATIVO);
        query = pedido.IdTrabalhadorPreferido is int preferido
            ? query.Where(t => t.IdUsuario == preferido)
            : query.Where(t => t.Atuacoes.Any(a => a.IdCategoria == pedido.IdCategoria));
        var ids = await query.Select(t => t.IdUsuario).ToListAsync();

        foreach (var idTrab in ids.Where(id => pedido.Convites.All(c => c.IdTrabalhador != id)))
        {
            _db.Convites.Add(new Convite
            {
                IdPedido = pedido.IdPedido,
                IdTrabalhador = idTrab,
                Status = StatusConvite.ENVIADO,
                DataEnvio = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        return ParaDto(pedido);
    }

    private static ConversaIaDto Conversa(Pedido p) => new(
        PedidoService.ParaDto(p),
        p.MensagensIa.OrderBy(m => m.IdMsgIa)
            .Select(m => new MensagemIaDto(m.IdMsgIa, m.Remetente.ToString(), m.Conteudo, m.DataHora)).ToList(),
        p.Briefing is null ? null : ParaDto(p));

    private static BriefingDto ParaDto(Pedido p) => new(
        p.Briefing!.IdBriefing,
        p.IdPedido,
        p.Categoria.Nome,
        p.Briefing.Resumo,
        Texto.Linhas(p.Briefing.Requisitos),
        p.Briefing.PrazoDesejado,
        p.Briefing.OrcamentoEstimado,
        p.Briefing.ConfirmadoCliente,
        p.Status.ToString());
}
