using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

public class TrabalhadoresController : BaseApiController
{
    private readonly TrabalhadorService _trabalhadores;

    public TrabalhadoresController(TrabalhadorService trabalhadores) => _trabalhadores = trabalhadores;

    /// <summary>"Encontre profissionais": ?busca=texto&amp;categoria=id</summary>
    [HttpGet]
    public async Task<ActionResult<List<TrabalhadorCardDto>>> Listar([FromQuery] string? busca, [FromQuery] int? categoria) =>
        Ok(await _trabalhadores.ListarAsync(busca, categoria));

    /// <summary>Home: "Mais Bem Avaliados".</summary>
    [HttpGet("destaques")]
    public async Task<ActionResult<List<TrabalhadorCardDto>>> Destaques() => Ok(await _trabalhadores.DestaquesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PerfilTrabalhadorDto>> Obter(int id) => Ok(await _trabalhadores.ObterPerfilAsync(id));

    /// <summary>"Meu perfil" do trabalhador logado.</summary>
    [Authorize(Roles = "TRABALHADOR")]
    [HttpGet("me")]
    public async Task<ActionResult<PerfilTrabalhadorDto>> MeuPerfil() => Ok(await _trabalhadores.ObterPerfilAsync(Usuario.Id));

    [Authorize(Roles = "TRABALHADOR")]
    [HttpPut("me")]
    public async Task<ActionResult<PerfilTrabalhadorDto>> Atualizar(AtualizarPerfilRequest req) =>
        Ok(await _trabalhadores.AtualizarPerfilAsync(Usuario.Id, req));
}
