using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize(Roles = "TRABALHADOR")]
public class ConvitesController : BaseApiController
{
    private readonly ConviteService _convites;

    public ConvitesController(ConviteService convites) => _convites = convites;

    /// <summary>?status=ENVIADO|ACEITO|RECUSADO</summary>
    [HttpGet]
    public async Task<ActionResult<List<ConviteDto>>> Listar([FromQuery] string? status) =>
        Ok(await _convites.ListarAsync(Usuario.Id, status));

    /// <summary>"Aceitar e abrir chat": devolve o id do chat criado.</summary>
    [HttpPost("{id:int}/aceitar")]
    public async Task<IActionResult> Aceitar(int id) => Ok(new { idChat = await _convites.AceitarAsync(Usuario.Id, id) });

    [HttpPost("{id:int}/recusar")]
    public async Task<IActionResult> Recusar(int id)
    {
        await _convites.RecusarAsync(Usuario.Id, id);
        return NoContent();
    }
}
