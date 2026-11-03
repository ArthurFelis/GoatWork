using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

public class CategoriaService
{
    private readonly IAppDbContext _db;

    public CategoriaService(IAppDbContext db) => _db = db;

    public Task<List<CategoriaDto>> ListarAsync() =>
        _db.Categorias.OrderBy(c => c.IdCategoria).Select(c => new CategoriaDto(c.IdCategoria, c.Nome)).ToListAsync();
}
