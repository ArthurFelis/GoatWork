namespace GoatWork.Domain.Exceptions;

/// <summary>Violação de regra de negócio (HTTP 400/409).</summary>
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}

/// <summary>Recurso inexistente (HTTP 404).</summary>
public class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string mensagem) : base(mensagem) { }
}

/// <summary>Usuário sem permissão sobre o recurso (HTTP 403).</summary>
public class AcessoNegadoException : Exception
{
    public AcessoNegadoException(string mensagem = "Você não tem acesso a este recurso.") : base(mensagem) { }
}
