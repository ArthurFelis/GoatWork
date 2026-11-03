using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

public class TermosController : BaseApiController
{
    private readonly TermoService _termos;

    public TermosController(TermoService termos) => _termos = termos;

    /// <summary>Versão vigente (telas Entrar, Cadastro e Termos de uso).</summary>
    [HttpGet("atual")]
    public async Task<ActionResult<TermoDto>> Atual() => Ok(await _termos.ObterVigenteAsync());

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<ActionResult<List<TermoDto>>> Listar() => Ok(await _termos.ListarAsync());

    /// <summary>Publica nova versão; os usuários aceitam de novo no próximo login.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<TermoDto>> Publicar(PublicarTermoRequest req) => Ok(await _termos.PublicarAsync(req));
}
