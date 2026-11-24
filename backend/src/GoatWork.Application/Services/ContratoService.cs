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

    /// <summary>
    /// Botão "Enviar orçamento" no chat (trabalhador): gera o contrato com o valor, a data, o horário
    /// e o local informados, JÁ ASSINADO pelo trabalhador. Enquanto o cliente não assinar,
    /// o trabalhador pode enviar um novo orçamento, que substitui o anterior.
    /// </summary>
    public async Task<ContratoDto> GerarAsync(UsuarioLogado u, GerarContratoRequest req)
    {
        var chat = await _db.Chats
                       .Include(c => c.Contrato)
                       .Include(c => c.Mensagens)
                       .Include(c => c.Convite).ThenInclude(cv => cv.Trabalhador)
                       .Include(c => c.Convite).ThenInclude(cv => cv.Pedido).ThenInclude(p => p.Briefing)
                       .FirstOrDefaultAsync(c => c.IdChat == req.IdChat)
                   ?? throw new NaoEncontradoException("Conversa não encontrada.");

        if (chat.Convite.IdTrabalhador != u.Id)
            throw new AcessoNegadoException("Só o profissional envia o orçamento.");
        Validacao.Exigir(chat.Status == StatusChat.ABERTO, "Esta conversa está encerrada.");
        Validacao.Exigir(req.Valor > 0, "Informe o valor do orçamento.");
        Validacao.Exigir(req.Valor <= 9_999_999m, "Valor muito alto.");
        Validacao.Exigir(req.Prazo >= DateOnly.FromDateTime(DateTime.Today), "A data do serviço não pode estar no passado.");

        var pedido = chat.Convite.Pedido;
        Validacao.Exigir(pedido.Status == StatusPedido.PUBLICADO, "Este pedido já foi fechado.");

        var hora = NormalizarHora(req.Hora);
        var local = string.IsNullOrWhiteSpace(req.Local) ? Texto.Local(pedido.Briefing?.Requisitos) : req.Local.Trim();
        if (local?.Length > 255) local = local[..255];
        var valor = Math.Round(req.Valor, 2);
        var agora = DateTime.Now;

        var contrato = chat.Contrato;
        if (contrato is null)
        {
            contrato = new Contrato { Chat = chat, DataGeracao = agora, TaxaPlataformaPerc = _plataforma.TaxaPercentualPadrao };
            _db.Contratos.Add(contrato);
        }
        else
        {
            Validacao.Exigir(contrato.AceiteClienteEm is null, "O cliente já assinou este contrato.");
        }

        contrato.ValorAcordado = valor;
        contrato.PrazoEntrega = req.Prazo;
        contrato.HoraServico = hora;
        contrato.LocalServico = local;
        contrato.TextoContrato = MontarTexto(pedido.Briefing, pedido.DescricaoInicial, valor, req.Prazo, hora, local);
        // O orçamento enviado já vale como assinatura do trabalhador
        contrato.AceiteTrabalhadorEm = agora;
        contrato.Status = StatusContrato.ACEITO_TRABALHADOR;

        // Fica registrado no chat (histórico para mediação)
        chat.Mensagens.Add(new Mensagem
        {
            IdRemetente = u.Id,
            Conteudo = $"Enviei o orçamento: {Texto.Moeda(valor)} para {DataComHora(req.Prazo, hora)}" +
                       (local is null ? "." : $" · {local}."),
            DataHora = agora
        });

        await _db.SaveChangesAsync();
        return await ObterAsync(u, contrato.IdContrato);
    }

    /// <summary>"Recusar orçamento" (cliente): descarta o contrato não assinado; a negociação continua.</summary>
    public async Task RecusarAsync(UsuarioLogado u, int idContrato)
    {
        var c = await CarregarAsync(u, idContrato);
        if (Papel(c, u.Id) != "CLIENTE") throw new AcessoNegadoException("Só o cliente pode recusar o orçamento.");
        Validacao.Exigir(c.AceiteClienteEm is null && c.Status is StatusContrato.GERADO or StatusContrato.ACEITO_TRABALHADOR,
            "Este contrato já foi assinado e não pode ser recusado.");

        c.Chat.Mensagens.Add(new Mensagem
        {
            IdRemetente = u.Id,
            Conteudo = $"Recusei o orçamento de {Texto.Moeda(c.ValorAcordado)}. Podemos ajustar?",
            DataHora = DateTime.Now
        });
        _db.Contratos.Remove(c);
        await _db.SaveChangesAsync();
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
        var pedido = c.Chat.Convite.Pedido;
        Validacao.Exigir(pedido.Status == StatusPedido.PUBLICADO, "Este pedido já foi fechado com outro contrato.");

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

        if (c.Status == StatusContrato.ATIVO)
        {
            // Pedido fechado: encerra as outras negociações e descarta orçamentos não assinados
            pedido.Status = StatusPedido.EM_ANDAMENTO;
            var outrosChats = await _db.Chats
                .Include(ch => ch.Contrato)
                .Where(ch => ch.Convite.IdPedido == pedido.IdPedido && ch.IdChat != c.IdChat)
                .ToListAsync();
            foreach (var outro in outrosChats)
            {
                outro.Status = StatusChat.FECHADO;
                if (outro.Contrato is { AceiteClienteEm: null } descartado) _db.Contratos.Remove(descartado);
            }
        }

        await _db.SaveChangesAsync();
        return ParaDto(c, u);
    }

    /// <summary>"9", "9h", "09:00", "14h30" → "9h", "9h", "9h", "14h30".</summary>
    public static string? NormalizarHora(string? hora)
    {
        if (string.IsNullOrWhiteSpace(hora)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(hora.Trim(), @"^(\d{1,2})\s*(?:[:hH]\s*(\d{2})?)?\s*h?$");
        if (!m.Success) throw new RegraNegocioException("Horário inválido. Use, por exemplo, 9h ou 14:30.");
        var h = int.Parse(m.Groups[1].Value);
        var min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
        if (h > 23 || min > 59) throw new RegraNegocioException("Horário inválido. Use, por exemplo, 9h ou 14:30.");
        return min == 0 ? $"{h}h" : $"{h}h{min:D2}";
    }

    public static string DataComHora(DateOnly data, string? hora) =>
        data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + (hora is null ? "" : $" às {hora}");

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

    private static string MontarTexto(Briefing? briefing, string descricao, decimal valor, DateOnly prazo, string? hora, string? local)
    {
        var objeto = briefing?.Resumo ?? Texto.TituloPedido(descricao);
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
        c.HoraServico,
        c.LocalServico,
        c.TaxaPlataformaPerc,
        c.AceiteClienteEm,
        c.AceiteTrabalhadorEm,
        u.EhAdmin ? "ADMIN" : Papel(c, u.Id)!,
        c.Pagamento?.Status.ToString(),
        Etapas.De(c));
}
