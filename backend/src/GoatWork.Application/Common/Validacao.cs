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

    public static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new RegraNegocioException(mensagem);
    }
}
