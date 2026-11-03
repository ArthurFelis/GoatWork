using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

/// <summary>Painel do administrador: mediação de disputas, usuários e contratos.</summary>
public class AdminService
{
    private readonly IAppDbContext _db;
    private readonly ContratoService _contratos;
    private readonly PagamentoService _pagamentos;

    public AdminService(IAppDbContext db, ContratoService contratos, PagamentoService pagamentos)
    {
        _db = db;
        _contratos = contratos;
        _pagamentos = pagamentos;
    }

    public async Task<AdminResumoDto> ResumoAsync() => new(
        await _db.Disputas.CountAsync(d => d.Status == StatusDisputa.ABERTA),
        await _db.Disputas.CountAsync(d => d.Status == StatusDisputa.EM_ANALISE),
        await _db.Disputas.CountAsync(d => d.Status == StatusDisputa.RESOLVIDA));

    public async Task<List<DisputaAdminItemDto>> ListarDisputasAsync(string? status)
    {
        var query = _db.Disputas.Include(d => d.Abertor).Include(d => d.Contrato).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusDisputa>(status, true, out var s))
            query = query.Where(d => d.Status == s);

        var lista = await query.OrderByDescending(d => d.DataAbertura).ThenByDescending(d => d.IdDisputa).ToListAsync();
        return lista.Select(Item).ToList();
    }

    public async Task<DisputaAdminDetalheDto> DetalheAsync(int idAdmin, int idDisputa)
    {
        var d = await CarregarDisputaAsync(idDisputa);
        var c = await _contratos.CarregarAsync(new UsuarioLogado(idAdmin, TipoUsuario.ADMIN), d.IdContrato);
        var idCliente = ContratoService.IdCliente(c);

        var chat = c.Chat.Mensagens.OrderBy(m => m.DataHora).ThenBy(m => m.IdMensagem)
            .Select(m =>
            {
                var doCliente = m.IdRemetente == idCliente;
                var nome = doCliente ? ContratoService.NomeCliente(c) : ContratoService.NomeTrabalhador(c);
                return new MensagemAdminDto(nome, doCliente, m.Conteudo, m.Bloqueada, m.DataHora);
            }).ToList();

        var n = 0;
        var entregas = c.Entregas.OrderBy(e => e.DataEntrega).ThenBy(e => e.IdEntrega)
            .Select(e => new EntregaDto(e.IdEntrega, ++n, e.Descricao, e.DataEntrega, e.Status.ToString())).ToList();

        return new DisputaAdminDetalheDto(
            d.IdDisputa,
            DisputaService.Numero(d.IdDisputa),
            c.IdContrato,
            c.Numero,
            d.Motivo,
            d.Status.ToString(),
            d.Decisao,
            ContratoService.NomeCliente(c),
            ContratoService.NomeTrabalhador(c),
            c.ValorAcordado,
            c.Pagamento?.Status.ToString() ?? StatusPagamento.PENDENTE.ToString(),
            chat,
            ContratoService.Clausulas(c.TextoContrato),
            entregas);
    }

    /// <summary>Botão "Analisar": a disputa passa para EM_ANALISE sob responsabilidade deste admin.</summary>
    public async Task<DisputaAdminDetalheDto> AnalisarAsync(int idAdmin, int idDisputa)
    {
        var d = await CarregarDisputaAsync(idDisputa);
        if (d.Status == StatusDisputa.ABERTA)
        {
            d.Status = StatusDisputa.EM_ANALISE;
            d.IdAdmin = idAdmin;
            await _db.SaveChangesAsync();
        }
        return await DetalheAsync(idAdmin, idDisputa);
    }

    /// <summary>"Registrar decisão": libera ao trabalhador ou estorna ao cliente.</summary>
    public async Task<DisputaAdminDetalheDto> DecidirAsync(int idAdmin, int idDisputa, DecidirDisputaRequest req)
    {
        var d = await CarregarDisputaAsync(idDisputa);
        Validacao.Exigir(d.Status is StatusDisputa.ABERTA or StatusDisputa.EM_ANALISE, "Esta disputa já foi resolvida.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Justificativa) && req.Justificativa.Trim().Length >= 10,
            "Registre a justificativa da decisão.");
        var liberar = string.Equals(req.Decisao, "LIBERAR", StringComparison.OrdinalIgnoreCase);
        var estornar = string.Equals(req.Decisao, "ESTORNAR", StringComparison.OrdinalIgnoreCase);
        Validacao.Exigir(liberar || estornar, "Escolha liberar ao trabalhador ou estornar ao cliente.");

        var c = await _contratos.CarregarAsync(new UsuarioLogado(idAdmin, TipoUsuario.ADMIN), d.IdContrato);
        Validacao.Exigir(c.Pagamento?.Status == StatusPagamento.EM_CUSTODIA, "O pagamento deste contrato não está em custódia.");

        if (liberar)
        {
            await _pagamentos.LiberarAsync(c.Pagamento!);
            foreach (var e in c.Entregas.Where(e => e.Status == StatusEntrega.PENDENTE)) e.Status = StatusEntrega.APROVADA;
            EntregaService.Concluir(c);
        }
        else
        {
            await _pagamentos.EstornarAsync(c.Pagamento!);
            foreach (var e in c.Entregas.Where(e => e.Status == StatusEntrega.PENDENTE)) e.Status = StatusEntrega.REJEITADA;
            c.Status = StatusContrato.CANCELADO;
            c.Chat.Status = StatusChat.FECHADO;
            c.Chat.Convite.Pedido.Status = StatusPedido.CANCELADO;
        }

        var (_, liquido) = Etapas.Valores(c.ValorAcordado, c.TaxaPlataformaPerc);
        d.Status = StatusDisputa.RESOLVIDA;
        d.IdAdmin = idAdmin;
        d.DataResolucao = DateTime.Now;
        d.Decisao = (liberar
            ? $"Pagamento liberado ao trabalhador ({Texto.Moeda(liquido)}). "
            : $"Pagamento estornado ao cliente ({Texto.Moeda(c.ValorAcordado)}). ") + "Justificativa: " + req.Justificativa.Trim();

        await _db.SaveChangesAsync();
        return await DetalheAsync(idAdmin, idDisputa);
    }

    public async Task<List<UsuarioAdminDto>> ListarUsuariosAsync(string? tipo)
    {
        var query = _db.Usuarios.Include(u => u.Trabalhador).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tipo) && Enum.TryParse<TipoUsuario>(tipo, true, out var t))
            query = query.Where(u => u.Tipo == t);
        var lista = await query.OrderByDescending(u => u.DataCadastro).ToListAsync();
        return lista.Select(u => new UsuarioAdminDto(u.IdUsuario, u.Nome, u.Email, u.Tipo.ToString(), u.Status.ToString(),
            u.Trabalhador?.Verificado, u.DataCadastro)).ToList();
    }

    public async Task VerificarTrabalhadorAsync(int idTrabalhador)
    {
        var t = await _db.Trabalhadores.FirstOrDefaultAsync(x => x.IdUsuario == idTrabalhador)
                ?? throw new NaoEncontradoException("Trabalhador não encontrado.");
        t.Verificado = true;
        t.DataVerificacao = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task AlterarStatusUsuarioAsync(int idAdmin, int idUsuario, AlterarStatusUsuarioRequest req)
    {
        Validacao.Exigir(idAdmin != idUsuario, "Você não pode alterar o próprio status.");
        if (!Enum.TryParse<StatusUsuario>(req.Status, true, out var status))
            throw new RegraNegocioException("Status inválido.");
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == idUsuario)
                ?? throw new NaoEncontradoException("Usuário não encontrado.");
        u.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task<List<ContratoAdminDto>> ListarContratosAsync()
    {
        var lista = await _contratos.ComDetalhes().AsSplitQuery().OrderByDescending(c => c.DataGeracao).ToListAsync();
        return lista.Select(c => new ContratoAdminDto(c.IdContrato, c.Numero, ContratoService.NomeCliente(c),
            ContratoService.NomeTrabalhador(c), c.ValorAcordado, c.Status.ToString(), c.Pagamento?.Status.ToString())).ToList();
    }

    private async Task<Disputa> CarregarDisputaAsync(int idDisputa) =>
        await _db.Disputas.Include(d => d.Abertor).Include(d => d.Contrato).FirstOrDefaultAsync(d => d.IdDisputa == idDisputa)
        ?? throw new NaoEncontradoException("Disputa não encontrada.");

    private static DisputaAdminItemDto Item(Disputa d) => new(
        d.IdDisputa,
        DisputaService.Numero(d.IdDisputa),
        d.IdContrato,
        d.Contrato.Numero,
        d.Abertor.Nome,
        d.Abertor.Tipo.ToString(),
        d.Motivo,
        d.Status.ToString());
}
