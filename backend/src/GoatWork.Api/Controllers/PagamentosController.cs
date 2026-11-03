using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize]
public class PagamentosController : BaseApiController
{
    private readonly PagamentoService _pagamentos;

    public PagamentosController(PagamentoService pagamentos) => _pagamentos = pagamentos;

    [HttpGet("contrato/{idContrato:int}")]
    public async Task<ActionResult<PagamentoResumoDto>> Obter(int idContrato) => Ok(await _pagamentos.ObterAsync(Usuario, idContrato));

    /// <summary>"Pagar R$ ...": o valor fica em custódia até a aprovação.</summary>
    [Authorize(Roles = "CLIENTE")]
    [HttpPost("contrato/{idContrato:int}")]
    public async Task<ActionResult<PagamentoResumoDto>> Pagar(int idContrato, PagarRequest req) =>
        Ok(await _pagamentos.PagarAsync(Usuario, idContrato, req));
}
