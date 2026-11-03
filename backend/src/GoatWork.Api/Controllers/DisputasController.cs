using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize]
public class DisputasController : BaseApiController
{
    private readonly DisputaService _disputas;

    public DisputasController(DisputaService disputas) => _disputas = disputas;

    [HttpGet("contrato/{idContrato:int}")]
    public async Task<ActionResult<DisputaContextoDto>> Obter(int idContrato) => Ok(await _disputas.ObterAsync(Usuario, idContrato));

    [Authorize(Roles = "CLIENTE,TRABALHADOR")]
    [HttpPost("contrato/{idContrato:int}")]
    public async Task<ActionResult<DisputaContextoDto>> Abrir(int idContrato, AbrirDisputaRequest req) =>
        Ok(await _disputas.AbrirAsync(Usuario, idContrato, req));
}
