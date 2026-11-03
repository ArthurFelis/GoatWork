using GoatWork.Application.DTOs;
using GoatWork.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoatWork.Api.Controllers;

public class CategoriasController : BaseApiController
{
    private readonly CategoriaService _categorias;

    public CategoriasController(CategoriaService categorias) => _categorias = categorias;

    [HttpGet]
    public async Task<ActionResult<List<CategoriaDto>>> Listar() => Ok(await _categorias.ListarAsync());
}
