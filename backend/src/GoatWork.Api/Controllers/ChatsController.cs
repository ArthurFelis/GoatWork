using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize(Roles = "CLIENTE,TRABALHADOR")]
public class ChatsController : BaseApiController
{
    private readonly ChatService _chats;

    public ChatsController(ChatService chats) => _chats = chats;

    [HttpGet]
    public async Task<ActionResult<List<ConversaResumoDto>>> Listar() => Ok(await _chats.ListarAsync(Usuario));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChatDto>> Obter(int id) => Ok(await _chats.ObterAsync(Usuario, id));

    /// <summary>Envia mensagem; contatos externos são bloqueados pelo FiltroContatoExterno.</summary>
    [HttpPost("{id:int}/mensagens")]
    public async Task<ActionResult<ChatDto>> Enviar(int id, EnviarMensagemRequest req) => Ok(await _chats.EnviarAsync(Usuario, id, req));
}
