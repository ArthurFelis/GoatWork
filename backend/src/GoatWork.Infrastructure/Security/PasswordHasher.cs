using GoatWork.Application.Interfaces;

namespace GoatWork.Infrastructure.Security;

/// <summary>Senhas são guardadas apenas como hash BCrypt (coluna senha_hash).</summary>
public class PasswordHasher : IPasswordHasher
{
    public string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 11);

    public bool Verificar(string senha, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(senha, hash); }
        catch { return false; }
    }
}
