using System.Text.RegularExpressions;
using GoatWork.Domain.Exceptions;

namespace GoatWork.Application.Common;

public static class Validacao
{
    public static string SoDigitos(string? valor) => Regex.Replace(valor ?? string.Empty, @"\D", "");

    public static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static bool CpfValido(string? cpf)
    {
        var d = SoDigitos(cpf);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        int Digito(int tamanho)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (d[i] - '0') * (tamanho + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Digito(9) == d[9] - '0' && Digito(10) == d[10] - '0';
    }

    /// <summary>
    /// Regras de senha (iguais às da tela de cadastro): 8+ caracteres, letra maiúscula,
    /// letra minúscula, número e caractere especial. Retorna o que falta, ou null se a senha é válida.
    /// </summary>
    public static string? ProblemaSenha(string? senha)
    {
        senha ??= string.Empty;
        var faltas = new List<string>();
        if (senha.Length < 8) faltas.Add("no mínimo 8 caracteres");
        if (!senha.Any(char.IsUpper)) faltas.Add("uma letra maiúscula");
        if (!senha.Any(char.IsLower)) faltas.Add("uma letra minúscula");
        if (!senha.Any(char.IsDigit)) faltas.Add("um número");
        if (!senha.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) faltas.Add("um caractere especial (ex.: @ # ! $)");
        if (senha.Any(char.IsWhiteSpace)) faltas.Add("sem espaços");
        if (senha.Length > 64) faltas.Add("no máximo 64 caracteres");
        return faltas.Count == 0 ? null : "A senha precisa ter " + string.Join(", ", faltas) + ".";
    }

    public static bool CnpjValido(string? cnpj)
    {
        var d = SoDigitos(cnpj);
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;
        int Digito(int tamanho)
        {
            int[] pesos = tamanho == 12
                ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
                : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (d[i] - '0') * pesos[i];
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Digito(12) == d[12] - '0' && Digito(13) == d[13] - '0';
    }

    public static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new RegraNegocioException(mensagem);
    }
}
