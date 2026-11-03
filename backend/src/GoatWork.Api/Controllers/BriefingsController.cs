using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

/// <summary>Conversa com a IA (MENSAGEM_IA) e revisão do briefing.</summary>
[Authorize]
public class BriefingsController : BaseApiController
{
    private readonly BriefingService _briefings;

    public BriefingsController(BriefingService briefings) => _briefings = briefings;

    [Authorize(Roles = "CLIENTE")]
    [HttpGet("pedido/{idPedido:int}/conversa")]
    public async Task<ActionResult<ConversaIaDto>> Conversa(int idPedido) =>
        Ok(await _briefings.ObterConversaAsync(Usuario.Id, idPedido));

    [Authorize(Roles = "CLIENTE")]
    [HttpPost("pedido/{idPedido:int}/mensagens")]
    public async Task<ActionResult<ConversaIaDto>> Enviar(int idPedido, EnviarMensagemRequest req) =>
        Ok(await _briefings.EnviarMensagemAsync(Usuario.Id, idPedido, req));

    [HttpGet("pedido/{idPedido:int}")]
    public async Task<ActionResult<BriefingDto>> Obter(int idPedido) =>
        Ok(await _briefings.ObterBriefingAsync(Usuario.Id, Usuario.Tipo, idPedido));

    /// <summary>"Confirmar e enviar pedido".</summary>
    [Authorize(Roles = "CLIENTE")]
    [HttpPut("pedido/{idPedido:int}/confirmar")]
    public async Task<ActionResult<BriefingDto>> Confirmar(int idPedido, ConfirmarBriefingRequest req) =>
        Ok(await _briefings.ConfirmarAsync(Usuario.Id, idPedido, req));
}
