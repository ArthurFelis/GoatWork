using System.Net;
using System.Text.Json;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Api.Middlewares;

/// <summary>Converte exceções em respostas JSON { "mensagem": "..." } com o status HTTP adequado.</summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, mensagem) = ex switch
            {
                RegraNegocioException => (HttpStatusCode.BadRequest, ex.Message),
                NaoEncontradoException => (HttpStatusCode.NotFound, ex.Message),
                AcessoNegadoException => (HttpStatusCode.Forbidden, ex.Message),
                DbUpdateException => (HttpStatusCode.Conflict, "Não foi possível salvar: os dados conflitam com um registro existente."),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado. Tente novamente.")
            };

            if (status == HttpStatusCode.InternalServerError || ex is DbUpdateException)
                _logger.LogError(ex, "Erro ao processar {Metodo} {Caminho}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensagem }));
        }
    }
}
