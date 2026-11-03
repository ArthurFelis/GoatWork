using System.Globalization;
using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Contrato automático (gerado a partir do briefing + acordo do chat) e assinaturas.</summary>
public class ContratoService
{
    private readonly IAppDbContext _db;
    private readonly PlataformaOptions _plataforma;

    public ContratoService(IAppDbContext db, PlataformaOptions plataforma)
    {
        _db = db;
        _plataforma = plataforma;
    }

    internal IQueryable<Contrato> ComDetalhes() =>
        _db.Contratos
            .Include(c => c.Pagamento)
            .Include(c => c.Entregas)
            .Include(c => c.Disputas)
            .Include(c => c.Avaliacoes)
            .Include(c => c.Chat).ThenInclude(ch => ch.Mensagens)
            .Include(c => c.Chat).ThenInclude(ch => ch.Convite).ThenInclude(cv => cv.Trabalhador).ThenInclude(t => t.Usuario)
            .Include(c => c.Chat).ThenInclude(ch => ch.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Briefing)
            .Include(c => c.Chat).ThenInclude(ch => ch.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Categoria)
            .Include(c => c.Chat).ThenInclude(ch => ch.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Cliente).ThenInclude(cl => cl.Usuario);

    /// <summary>Carrega o contrato garantindo que o usuário é uma das partes (ou admin).</summary>
    internal async Task<Contrato> CarregarAsync(UsuarioLogado u, int idContrato)
    {
        var c = await ComDetalhes().AsSplitQuery().FirstOrDefaultAsync(x => x.IdContrato == idContrato)
                ?? throw new NaoEncontradoException("Contrato não encontrado.");
        if (!u.EhAdmin && Papel(c, u.Id) is null) throw new AcessoNegadoException();
        return c;
    }

    public static int IdCliente(Contrato c) => c.Chat.Convite.Pedido.IdCliente;
    public static int IdTrabalhador(Contrato c) => c.Chat.Convite.IdTrabalhador;
    public static string NomeCliente(Contrato c) => c.Chat.Convite.Pedido.Cliente.NomeExibicao ?? c.Chat.Convite.Pedido.Cliente.Usuario.Nome;
    public static string NomeTrabalhador(Contrato c) => c.Chat.Convite.Trabalhador.Usuario.Nome;
    public static string Resumo(Contrato c) => c.Chat.Convite.Pedido.Briefing?.Resumo ?? Texto.TituloPedido(c.Chat.Convite.Pedido.DescricaoInicial);

    public static string? Papel(Contrato c, int idUsuario) =>
        IdCliente(c) == idUsuario ? "CLIENTE" : IdTrabalhador(c) == idUsuario ? "TRABALHADOR" : null;

    /// <summary>Card "Acordo identificado" → "Gerar contrato".</summary>
    public async Task<ContratoDto> GerarAsync(UsuarioLogado u, GerarContratoRequest req)
    {
        var chat = await _db.Chats
                       .Include(c => c.Contrato)
                       .Include(c => c.Mensagens)
                       .Include(c => c.Convite).ThenInclude(cv => cv.Trabalhador)
                       .Include(c => c.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Briefing)
                       .Include(c => c.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Convites).ThenInclude(o => o.Chat)
                       .FirstOrDefaultAsync(c => c.IdChat == req.IdChat)
                   ?? throw new NaoEncontradoException("Conversa não encontrada.");

        var participa = chat.Convite.IdTrabalhador == u.Id || chat.Convite.Pedido.IdCliente == u.Id;
        if (!participa) throw new AcessoNegadoException();
        Validacao.Exigir(chat.Contrato is null, "Esta conversa já tem um contrato.");
        Validacao.Exigir(chat.Status == StatusChat.ABERTO, "Esta conversa está encerrada.");
        Validacao.Exigir(req.Valor > 0, "Informe o valor acordado.");
        Validacao.Exigir(req.Prazo >= DateOnly.FromDateTime(DateTime.Today), "A data de entrega não pode estar no passado.");

        var pedido = chat.Convite.Pedido;
        Validacao.Exigir(pedido.Status == StatusPedido.PUBLICADO, "Este pedido já tem um contrato em andamento.");

        var textos = chat.Mensagens.Where(m => !m.Bloqueada).OrderBy(m => m.DataHora).Select(m => m.Conteudo);
        var hora = textos.Reverse().Select(DetectorAcordo.ExtrairHora).FirstOrDefault(h => h is not null);

        var contrato = new Contrato
        {
            Chat = chat,
            ValorAcordado = Math.Round(req.Valor, 2),
            PrazoEntrega = req.Prazo,
            TaxaPlataformaPerc = _plataforma.TaxaPercentualPadrao,
            Status = StatusContrato.GERADO,
            DataGeracao = DateTime.Now,
            TextoContrato = MontarTexto(pedido.Briefing, pedido.DescricaoInicial, req.Valor, req.Prazo, hora)
        };
        _db.Contratos.Add(contrato);

        pedido.Status = StatusPedido.EM_ANDAMENTO;
        // As outras conversas do mesmo pedido são encerradas
        foreach (var outro in pedido.Convites.Where(c => c.Chat is not null && c.Chat.IdChat != chat.IdChat))
            outro.Chat!.Status = StatusChat.FECHADO;

        await _db.SaveChangesAsync();
        return await ObterAsync(u, contrato.IdContrato);
    }

    public async Task<ContratoDto> ObterAsync(UsuarioLogado u, int idContrato) =>
        ParaDto(await CarregarAsync(u, idContrato), u);

    /// <summary>Botão "Assinar" (regra 4: contrato só vale com o aceite das duas partes).</summary>
    public async Task<ContratoDto> AssinarAsync(UsuarioLogado u, int idContrato)
    {
        var c = await CarregarAsync(u, idContrato);
        var papel = Papel(c, u.Id) ?? throw new AcessoNegadoException("Só as partes do contrato podem assinar.");
        Validacao.Exigir(c.Status is StatusContrato.GERADO or StatusContrato.ACEITO_CLIENTE or StatusContrato.ACEITO_TRABALHADOR,
            "Este contrato não está aguardando assinatura.");

        var agora = DateTime.Now;
        if (papel == "CLIENTE")
        {
            Validacao.Exigir(c.AceiteClienteEm is null, "Você já assinou este contrato.");
            c.AceiteClienteEm = agora;
        }
        else
        {
            Validacao.Exigir(c.AceiteTrabalhadorEm is null, "Você já assinou este contrato.");
            c.AceiteTrabalhadorEm = agora;
        }

        c.Status = (c.AceiteClienteEm, c.AceiteTrabalhadorEm) switch
        {
            (not null, not null) => StatusContrato.ATIVO,
            (not null, null) => StatusContrato.ACEITO_CLIENTE,
            (null, not null) => StatusContrato.ACEITO_TRABALHADOR,
            _ => StatusContrato.GERADO
        };
        await _db.SaveChangesAsync();
        return ParaDto(c, u);
    }

    /// <summary>Tabela "Contratos" da tela "Pedidos e contratos".</summary>
    public async Task<List<ContratoListaDto>> ListarAsync(UsuarioLogado u)
    {
        var query = ComDetalhes().AsSplitQuery();
        query = u.EhTrabalhador
            ? query.Where(c => c.Chat.Convite.IdTrabalhador == u.Id)
            : query.Where(c => c.Chat.Convite.Pedido.IdCliente == u.Id);

        var contratos = await query.OrderByDescending(c => c.DataGeracao).ToListAsync();
        return contratos.Select(c => new ContratoListaDto(
            c.IdContrato,
            c.Numero,
            Texto.TituloPedido(c.Chat.Convite.Pedido.DescricaoInicial),
            u.EhTrabalhador ? NomeCliente(c) : NomeTrabalhador(c),
            c.ValorAcordado,
            c.Status.ToString(),
            Etapas.De(c),
            u.EhTrabalhador ? c.AceiteTrabalhadorEm is not null : c.AceiteClienteEm is not null,
            c.Avaliacoes.Any(a => a.IdAvaliador == u.Id),
            c.Disputas.OrderByDescending(d => d.DataAbertura).FirstOrDefault()?.IdDisputa)).ToList();
    }

    public static List<ClausulaDto> Clausulas(string texto) =>
        texto.Replace("\r", "")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(bloco =>
            {
                var i = bloco.IndexOf('\n');
                return i < 0 ? new ClausulaDto(bloco, string.Empty) : new ClausulaDto(bloco[..i].Trim(), bloco[(i + 1)..].Trim());
            })
            .ToList();

    private static string MontarTexto(Briefing? briefing, string descricao, decimal valor, DateOnly prazo, string? hora)
    {
        var objeto = briefing?.Resumo ?? Texto.TituloPedido(descricao);
        var local = Texto.Local(briefing?.Requisitos);
        var remoto = local is null || local.Contains("remot", StringComparison.OrdinalIgnoreCase);
        var dia = Texto.Capitalizar(prazo.ToString("dddd", Texto.PtBr).Replace("-feira", ""));
        var data = $"{dia}, {prazo.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}{(hora is null ? "" : $", às {hora}")}";

        var clausulas = new[]
        {
            ("Cláusula 1 — Objeto", objeto.TrimEnd('.') + "."),
            ("Cláusula 2 — Local e data", remoto ? $"Serviço remoto, pelo chat da GoatWork. Entrega até {data}." : $"{local!.TrimEnd('.')}. {data}."),
            ("Cláusula 3 — Valor", $"{Texto.Moeda(valor)}, pagos pela GoatWork e retidos até a conclusão do serviço."),
            ("Cláusula 4 — Obrigações do trabalhador", "Executar o serviço conforme descrito, com materiais e segurança adequados."),
            ("Cláusula 5 — Obrigações do cliente", remoto
                ? "Fornecer as informações necessárias e confirmar a conclusão em até 48h."
                : "Garantir acesso ao local e confirmar a conclusão em até 48h."),
            ("Cláusula 6 — Responsabilidade", "A GoatWork atua apenas como intermediadora e não responde por danos ocorridos durante o serviço."),
            ("Cláusula 7 — Boa-fé", "As partes declaram agir de boa-fé. Disputas serão mediadas com base no chat e neste contrato.")
        };
        return string.Join("\n\n", clausulas.Select(c => $"{c.Item1}\n{c.Item2}"));
    }

    public static ContratoDto ParaDto(Contrato c, UsuarioLogado u) => new(
        c.IdContrato,
        c.Numero,
        c.Status.ToString(),
        c.IdChat,
        c.Chat.Convite.IdPedido,
        Resumo(c),
        NomeCliente(c),
        NomeTrabalhador(c),
        c.Chat.Convite.Trabalhador.Profissao,
        Clausulas(c.TextoContrato),
        c.ValorAcordado,
        c.PrazoEntrega,
        c.TaxaPlataformaPerc,
        c.AceiteClienteEm,
        c.AceiteTrabalhadorEm,
        u.EhAdmin ? "ADMIN" : Papel(c, u.Id)!,
        c.Pagamento?.Status.ToString(),
        Etapas.De(c));
}
