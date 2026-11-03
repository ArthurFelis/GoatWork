using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

public class UsuarioService
{
    private readonly IAppDbContext _db;

    public UsuarioService(IAppDbContext db) => _db = db;

    public async Task<UsuarioResumoDto> AtualizarAsync(int idUsuario, AtualizarUsuarioRequest req)
    {
        var u = await _db.Usuarios.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.IdUsuario == idUsuario)
                ?? throw new NaoEncontradoException("Usuário não encontrado.");
        if (string.IsNullOrWhiteSpace(req.Nome) || req.Nome.Trim().Length < 3)
            throw new RegraNegocioException("Informe seu nome completo.");
        u.Nome = req.Nome.Trim();
        u.Telefone = string.IsNullOrWhiteSpace(req.Telefone) ? null : req.Telefone.Trim();
        await _db.SaveChangesAsync();
        return AuthService.Resumo(u);
    }
}
