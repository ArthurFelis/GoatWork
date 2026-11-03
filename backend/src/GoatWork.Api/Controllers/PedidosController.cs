using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize(Roles = "CLIENTE")]
public class PedidosController : BaseApiController
{
    private readonly PedidoService _pedidos;

    public PedidosController(PedidoService pedidos) => _pedidos = pedidos;

    [HttpGet]
    public async Task<ActionResult<List<PedidoDto>>> Listar() => Ok(await _pedidos.ListarDoClienteAsync(Usuario.Id));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDto>> Obter(int id) => Ok(await _pedidos.ObterAsync(Usuario.Id, id));

    /// <summary>"Salvar rascunho" (rascunho = true) ou "Continuar com a IA" (rascunho = false).</summary>
    [HttpPost]
    public async Task<ActionResult<PedidoDto>> Criar(CriarPedidoRequest req) => Ok(await _pedidos.CriarAsync(Usuario.Id, req));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PedidoDto>> Atualizar(int id, AtualizarPedidoRequest req) =>
        Ok(await _pedidos.AtualizarAsync(Usuario.Id, id, req));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        await _pedidos.CancelarAsync(Usuario.Id, id);
        return NoContent();
    }
}
