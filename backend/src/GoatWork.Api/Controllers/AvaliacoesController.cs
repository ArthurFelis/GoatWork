using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize(Roles = "CLIENTE,TRABALHADOR")]
public class AvaliacoesController : BaseApiController
{
    private readonly AvaliacaoService _avaliacoes;

    public AvaliacoesController(AvaliacaoService avaliacoes) => _avaliacoes = avaliacoes;

    [HttpGet("contrato/{idContrato:int}")]
    public async Task<ActionResult<AvaliacaoContextoDto>> Obter(int idContrato) => Ok(await _avaliacoes.ObterAsync(Usuario, idContrato));

    [HttpPost("contrato/{idContrato:int}")]
    public async Task<ActionResult<AvaliacaoContextoDto>> Avaliar(int idContrato, AvaliarRequest req) =>
        Ok(await _avaliacoes.AvaliarAsync(Usuario, idContrato, req));
}
