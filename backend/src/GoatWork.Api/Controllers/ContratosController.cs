using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize]
public class ContratosController : BaseApiController
{
    private readonly ContratoService _contratos;

    public ContratosController(ContratoService contratos) => _contratos = contratos;

    [Authorize(Roles = "CLIENTE,TRABALHADOR")]
    [HttpGet]
    public async Task<ActionResult<List<ContratoListaDto>>> Listar() => Ok(await _contratos.ListarAsync(Usuario));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContratoDto>> Obter(int id) => Ok(await _contratos.ObterAsync(Usuario, id));

    /// <summary>"Gerar contrato" a partir do acordo identificado no chat.</summary>
    [Authorize(Roles = "CLIENTE,TRABALHADOR")]
    [HttpPost]
    public async Task<ActionResult<ContratoDto>> Gerar(GerarContratoRequest req) => Ok(await _contratos.GerarAsync(Usuario, req));

    [Authorize(Roles = "CLIENTE,TRABALHADOR")]
    [HttpPost("{id:int}/assinar")]
    public async Task<ActionResult<ContratoDto>> Assinar(int id) => Ok(await _contratos.AssinarAsync(Usuario, id));
}
