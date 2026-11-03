using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Tela "Criar conta na GoatWork".</summary>
    [HttpPost("cadastro")]
    public async Task<ActionResult<AuthResponse>> Cadastrar(CadastroRequest req) => Ok(await _auth.CadastrarAsync(req, Ip));

    /// <summary>Tela "Entrar na GoatWork".</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Entrar(LoginRequest req) => Ok(await _auth.EntrarAsync(req, Ip));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioResumoDto>> Eu() => Ok(await _auth.ObterAsync(Usuario.Id));
}
