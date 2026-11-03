using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize]
public class EntregasController : BaseApiController
{
    private readonly EntregaService _entregas;

    public EntregasController(EntregaService entregas) => _entregas = entregas;

    [HttpGet("contrato/{idContrato:int}")]
    public async Task<ActionResult<EntregasContratoDto>> Obter(int idContrato) => Ok(await _entregas.ObterAsync(Usuario, idContrato));

    [Authorize(Roles = "TRABALHADOR")]
    [HttpPost("contrato/{idContrato:int}")]
    public async Task<ActionResult<EntregasContratoDto>> Enviar(int idContrato, EnviarEntregaRequest req) =>
        Ok(await _entregas.EnviarAsync(Usuario, idContrato, req));

    [Authorize(Roles = "CLIENTE")]
    [HttpPost("{id:int}/aprovar")]
    public async Task<ActionResult<EntregasContratoDto>> Aprovar(int id) => Ok(await _entregas.AprovarAsync(Usuario, id));

    [Authorize(Roles = "CLIENTE")]
    [HttpPost("{id:int}/rejeitar")]
    public async Task<ActionResult<EntregasContratoDto>> Rejeitar(int id) => Ok(await _entregas.RejeitarAsync(Usuario, id));
}
