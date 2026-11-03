using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

public class TrabalhadorService
{
    private static readonly string[] Unidades = { "h", "dia", "evento", "sessão", "serviço" };
    private readonly IAppDbContext _db;

    public TrabalhadorService(IAppDbContext db) => _db = db;

    private IQueryable<Trabalhador> Ativos() =>
        _db.Trabalhadores
            .Include(t => t.Usuario)
            .Include(t => t.Atuacoes).ThenInclude(a => a.Categoria)
            .Where(t => t.Usuario.Status == StatusUsuario.ATIVO);

    /// <summary>Tela "Encontre profissionais": busca por nome/serviço e filtro por categoria.</summary>
    public async Task<List<TrabalhadorCardDto>> ListarAsync(string? busca, int? idCategoria)
    {
        var query = Ativos();
        if (idCategoria is > 0)
            query = query.Where(t => t.Atuacoes.Any(a => a.IdCategoria == idCategoria));
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim();
            query = query.Where(t =>
                t.Usuario.Nome.Contains(b) ||
                (t.Profissao != null && t.Profissao.Contains(b)) ||
                (t.Biografia != null && t.Biografia.Contains(b)) ||
                t.Atuacoes.Any(a => a.Categoria.Nome.Contains(b)));
        }

        // Ordem de cadastro (a mesma da tela do Figma); a nota aparece em cada card
        var lista = await query.OrderBy(t => t.IdUsuario).ToListAsync();
        return lista.Select(Card).ToList();
    }

    /// <summary>Home: "Mais Bem Avaliados".</summary>
    public async Task<List<TrabalhadorCardDto>> DestaquesAsync(int quantidade = 4)
    {
        var lista = await Ativos()
            .Where(t => t.ServicosConcluidos > 0)
            .OrderByDescending(t => t.NotaMedia)
            .ThenByDescending(t => t.ServicosConcluidos)
            .Take(quantidade)
            .ToListAsync();
        return lista.Select(Card).ToList();
    }

    public async Task<PerfilTrabalhadorDto> ObterPerfilAsync(int idTrabalhador)
    {
        var t = await Ativos().FirstOrDefaultAsync(x => x.IdUsuario == idTrabalhador)
                ?? throw new NaoEncontradoException("Profissional não encontrado.");
        return Perfil(t);
    }

    public async Task<PerfilTrabalhadorDto> AtualizarPerfilAsync(int idTrabalhador, AtualizarPerfilRequest req)
    {
        var t = await _db.Trabalhadores
                    .Include(x => x.Usuario)
                    .Include(x => x.Atuacoes).ThenInclude(a => a.Categoria)
                    .FirstOrDefaultAsync(x => x.IdUsuario == idTrabalhador)
                ?? throw new NaoEncontradoException("Perfil de trabalhador não encontrado.");

        var atuacoes = req.Atuacoes ?? new List<AtuacaoRequest>();
        Validacao.Exigir(atuacoes.Count > 0, "Adicione pelo menos uma categoria de atuação.");
        Validacao.Exigir(atuacoes.Select(a => a.IdCategoria).Distinct().Count() == atuacoes.Count, "Cada categoria só pode aparecer uma vez.");
        Validacao.Exigir(atuacoes.All(a => a.AnosExperiencia is >= 0 and <= 80), "Anos de experiência inválidos.");
        Validacao.Exigir(req.ValorReferencia is null or >= 0m, "Valor de referência inválido.");
        Validacao.Exigir((req.Biografia ?? "").Length <= 1000, "A biografia pode ter no máximo 1000 caracteres.");

        var ids = atuacoes.Select(a => a.IdCategoria).ToList();
        var existentes = await _db.Categorias.Where(c => ids.Contains(c.IdCategoria)).CountAsync();
        Validacao.Exigir(existentes == ids.Count, "Categoria inválida.");

        t.Biografia = string.IsNullOrWhiteSpace(req.Biografia) ? null : req.Biografia.Trim();
        t.Profissao = string.IsNullOrWhiteSpace(req.Profissao) ? null : req.Profissao.Trim();
        t.Cidade = string.IsNullOrWhiteSpace(req.Cidade) ? null : req.Cidade.Trim();
        t.ValorReferencia = req.ValorReferencia;
        t.UnidadeValor = Unidades.Contains(req.UnidadeValor) ? req.UnidadeValor : "h";

        // Sincroniza ATUACAO (remove as que saíram, atualiza e adiciona)
        foreach (var antiga in t.Atuacoes.Where(a => !ids.Contains(a.IdCategoria)).ToList())
            _db.Atuacoes.Remove(antiga);
        foreach (var a in atuacoes)
        {
            var atual = t.Atuacoes.FirstOrDefault(x => x.IdCategoria == a.IdCategoria);
            if (atual is null)
                _db.Atuacoes.Add(new Atuacao { IdTrabalhador = t.IdUsuario, IdCategoria = a.IdCategoria, AnosExperiencia = (byte)a.AnosExperiencia });
            else
                atual.AnosExperiencia = (byte)a.AnosExperiencia;
        }

        await _db.SaveChangesAsync();
        return await ObterPerfilAsync(idTrabalhador);
    }

    public static TrabalhadorCardDto Card(Trabalhador t) => new(
        t.IdUsuario,
        t.Usuario.Nome,
        Texto.Iniciais(t.Usuario.Nome),
        t.Profissao,
        t.Cidade,
        t.Biografia,
        t.NotaMedia,
        t.ServicosConcluidos,
        t.ValorReferencia,
        t.UnidadeValor,
        t.Verificado,
        t.Atuacoes.Select(a => a.IdCategoria).ToList(),
        t.Atuacoes.Select(a => a.Categoria.Nome).ToList());

    public static PerfilTrabalhadorDto Perfil(Trabalhador t) => new(
        t.IdUsuario,
        t.Usuario.Nome,
        Texto.Iniciais(t.Usuario.Nome),
        t.Profissao,
        t.Cidade,
        t.Biografia,
        t.NotaMedia,
        t.ServicosConcluidos,
        t.ValorReferencia,
        t.UnidadeValor,
        t.Verificado,
        t.DataVerificacao,
        t.Atuacoes.OrderByDescending(a => a.AnosExperiencia)
            .Select(a => new AtuacaoDto(a.IdCategoria, a.Categoria.Nome, a.AnosExperiencia)).ToList());
}
