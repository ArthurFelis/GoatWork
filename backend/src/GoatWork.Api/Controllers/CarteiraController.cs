using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

/// <summary>Tela "Minha carteira": saldo e saques do trabalhador.</summary>
[Authorize(Roles = "TRABALHADOR")]
public class CarteiraController : BaseApiController
{
    private readonly CarteiraService _carteira;

    public CarteiraController(CarteiraService carteira) => _carteira = carteira;

    [HttpGet]
    public async Task<ActionResult<CarteiraDto>> Obter() => Ok(await _carteira.ObterAsync(Usuario.Id));

    /// <summary>"Retirar": saque via Pix do saldo disponível.</summary>
    [HttpPost("saques")]
    public async Task<ActionResult<CarteiraDto>> Sacar(SacarRequest req) => Ok(await _carteira.SacarAsync(Usuario.Id, req));
}
