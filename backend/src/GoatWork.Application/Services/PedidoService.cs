using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

public class PedidoService
{
    private readonly IAppDbContext _db;
    private readonly BriefingService _briefings;

    public PedidoService(IAppDbContext db, BriefingService briefings)
    {
        _db = db;
        _briefings = briefings;
    }

    internal IQueryable<Pedido> ComDetalhes() =>
        _db.Pedidos
            .Include(p => p.Categoria)
            .Include(p => p.Briefing)
            .Include(p => p.TrabalhadorPreferido!).ThenInclude(t => t.Usuario)
            .Include(p => p.Convites).ThenInclude(c => c.Trabalhador).ThenInclude(t => t.Usuario)
            .Include(p => p.Convites).ThenInclude(c => c.Chat!).ThenInclude(ch => ch.Contrato);

    /// <summary>Tela "Novo pedido": "Salvar rascunho" ou "Continuar com a IA".</summary>
    public async Task<PedidoDto> CriarAsync(int idCliente, CriarPedidoRequest req)
    {
        await ValidarAsync(req.IdCategoria, req.Descricao);

        int? preferido = null;
        if (req.IdTrabalhador is > 0)
        {
            var existe = await _db.Trabalhadores.AnyAsync(t => t.IdUsuario == req.IdTrabalhador && t.Usuario.Status == StatusUsuario.ATIVO);
            Validacao.Exigir(existe, "Profissional não encontrado.");
            preferido = req.IdTrabalhador;
        }

        var pedido = new Pedido
        {
            IdCliente = idCliente,
            IdCategoria = req.IdCategoria,
            DescricaoInicial = req.Descricao.Trim(),
            IdTrabalhadorPreferido = preferido,
            Status = req.Rascunho ? StatusPedido.RASCUNHO : StatusPedido.EM_BRIEFING,
            DataCriacao = DateTime.Now
        };
        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();

        if (!req.Rascunho) await _briefings.IniciarConversaAsync(pedido.IdPedido);
        return await ObterAsync(idCliente, pedido.IdPedido);
    }

    /// <summary>Continua um rascunho (mesma tela "Novo pedido").</summary>
    public async Task<PedidoDto> AtualizarAsync(int idCliente, int idPedido, AtualizarPedidoRequest req)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.IdPedido == idPedido)
                     ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (pedido.IdCliente != idCliente) throw new AcessoNegadoException();
        Validacao.Exigir(pedido.Status == StatusPedido.RASCUNHO, "Só é possível editar pedidos em rascunho.");
        await ValidarAsync(req.IdCategoria, req.Descricao);

        pedido.IdCategoria = req.IdCategoria;
        pedido.DescricaoInicial = req.Descricao.Trim();
        if (!req.Rascunho) pedido.Status = StatusPedido.EM_BRIEFING;
        await _db.SaveChangesAsync();

        if (!req.Rascunho) await _briefings.IniciarConversaAsync(pedido.IdPedido);
        return await ObterAsync(idCliente, pedido.IdPedido);
    }

    public async Task<List<PedidoDto>> ListarDoClienteAsync(int idCliente)
    {
        var pedidos = await ComDetalhes()
            .Where(p => p.IdCliente == idCliente && p.Status != StatusPedido.CANCELADO)
            .OrderByDescending(p => p.DataCriacao)
            .ToListAsync();
        return pedidos.Select(ParaDto).ToList();
    }

    public async Task<PedidoDto> ObterAsync(int idCliente, int idPedido)
    {
        var p = await ComDetalhes().FirstOrDefaultAsync(x => x.IdPedido == idPedido)
                ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (p.IdCliente != idCliente) throw new AcessoNegadoException();
        return ParaDto(p);
    }

    public async Task CancelarAsync(int idCliente, int idPedido)
    {
        var p = await _db.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == idPedido)
                ?? throw new NaoEncontradoException("Pedido não encontrado.");
        if (p.IdCliente != idCliente) throw new AcessoNegadoException();
        Validacao.Exigir(p.Status is StatusPedido.RASCUNHO or StatusPedido.EM_BRIEFING or StatusPedido.PUBLICADO,
            "Este pedido já está em andamento e não pode ser cancelado.");
        p.Status = StatusPedido.CANCELADO;
        await _db.SaveChangesAsync();
    }

    private async Task ValidarAsync(int idCategoria, string? descricao)
    {
        Validacao.Exigir(await _db.Categorias.AnyAsync(c => c.IdCategoria == idCategoria), "Escolha a categoria do serviço.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(descricao) && descricao.Trim().Length >= 10, "Conte em poucas palavras o que você precisa.");
        Validacao.Exigir(descricao!.Length <= 2000, "A descrição pode ter no máximo 2000 caracteres.");
    }

    public static PedidoDto ParaDto(Pedido p)
    {
        var aceito = p.Convites.FirstOrDefault(c => c.Status == StatusConvite.ACEITO && c.Chat != null);
        return new PedidoDto(
            p.IdPedido,
            Texto.NumeroPedido(p.IdPedido),
            Texto.TituloPedido(p.DescricaoInicial),
            p.DescricaoInicial,
            p.IdCategoria,
            p.Categoria.Nome,
            p.Status.ToString(),
            p.DataCriacao,
            p.IdTrabalhadorPreferido,
            p.TrabalhadorPreferido?.Usuario.Nome,
            aceito?.Chat?.IdChat,
            aceito?.Trabalhador.Usuario.Nome,
            aceito?.Chat?.Contrato?.IdContrato,
            p.Briefing is not null);
    }
}
