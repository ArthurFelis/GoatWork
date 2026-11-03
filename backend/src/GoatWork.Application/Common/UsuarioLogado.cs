using GoatWork.Domain.Enums;

namespace GoatWork.Application.Common;

/// <summary>Quem está fazendo a requisição (vem do token JWT).</summary>
public record UsuarioLogado(int Id, TipoUsuario Tipo)
{
    public bool EhCliente => Tipo == TipoUsuario.CLIENTE;
    public bool EhTrabalhador => Tipo == TipoUsuario.TRABALHADOR;
    public bool EhAdmin => Tipo == TipoUsuario.ADMIN;
}
