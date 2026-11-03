using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminController : BaseApiController
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin) => _admin = admin;

    [HttpGet("resumo")]
    public async Task<ActionResult<AdminResumoDto>> Resumo() => Ok(await _admin.ResumoAsync());

    /// <summary>?status=ABERTA|EM_ANALISE|RESOLVIDA</summary>
    [HttpGet("disputas")]
    public async Task<ActionResult<List<DisputaAdminItemDto>>> Disputas([FromQuery] string? status) =>
        Ok(await _admin.ListarDisputasAsync(status));

    [HttpGet("disputas/{id:int}")]
    public async Task<ActionResult<DisputaAdminDetalheDto>> Disputa(int id) => Ok(await _admin.DetalheAsync(Usuario.Id, id));

    [HttpPost("disputas/{id:int}/analisar")]
    public async Task<ActionResult<DisputaAdminDetalheDto>> Analisar(int id) => Ok(await _admin.AnalisarAsync(Usuario.Id, id));

    [HttpPost("disputas/{id:int}/decidir")]
    public async Task<ActionResult<DisputaAdminDetalheDto>> Decidir(int id, DecidirDisputaRequest req) =>
        Ok(await _admin.DecidirAsync(Usuario.Id, id, req));

    [HttpGet("usuarios")]
    public async Task<ActionResult<List<UsuarioAdminDto>>> Usuarios([FromQuery] string? tipo) => Ok(await _admin.ListarUsuariosAsync(tipo));

    [HttpPost("usuarios/{id:int}/verificar")]
    public async Task<IActionResult> Verificar(int id)
    {
        await _admin.VerificarTrabalhadorAsync(id);
        return NoContent();
    }

    [HttpPost("usuarios/{id:int}/status")]
    public async Task<IActionResult> AlterarStatus(int id, AlterarStatusUsuarioRequest req)
    {
        await _admin.AlterarStatusUsuarioAsync(Usuario.Id, id, req);
        return NoContent();
    }

    [HttpGet("contratos")]
    public async Task<ActionResult<List<ContratoAdminDto>>> Contratos() => Ok(await _admin.ListarContratosAsync());
}
