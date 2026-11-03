using System.Security.Claims;
using GoatWork.Application.Common;
using GoatWork.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>Usuário autenticado, lido das claims do JWT.</summary>
    protected UsuarioLogado Usuario => new(
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0"),
        Enum.Parse<TipoUsuario>(User.FindFirstValue(ClaimTypes.Role) ?? nameof(TipoUsuario.CLIENTE)));

    protected string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
}
