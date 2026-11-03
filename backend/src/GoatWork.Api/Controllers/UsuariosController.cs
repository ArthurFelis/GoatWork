using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize]
public class UsuariosController : BaseApiController
{
    private readonly UsuarioService _usuarios;
    private readonly AuthService _auth;

    public UsuariosController(UsuarioService usuarios, AuthService auth)
    {
        _usuarios = usuarios;
        _auth = auth;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UsuarioResumoDto>> Eu() => Ok(await _auth.ObterAsync(Usuario.Id));

    [HttpPut("me")]
    public async Task<ActionResult<UsuarioResumoDto>> Atualizar(AtualizarUsuarioRequest req) =>
        Ok(await _usuarios.AtualizarAsync(Usuario.Id, req));
}
