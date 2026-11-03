using System.Globalization;
using System.Text.RegularExpressions;

namespace GoatWork.Application.Common;

/// <summary>Helpers de texto usados para montar títulos, iniciais e campos exibidos nas telas.</summary>
public static class Texto
{
    public static readonly CultureInfo PtBr = new("pt-BR");

    public static string Iniciais(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
    }

    /// <summary>"Preciso instalar 4 tomadas..." → "Instalar 4 tomadas..." (máx. ~60 caracteres).</summary>
    public static string TituloPedido(string? descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao)) return "Pedido";
        var t = descricao.Trim();
        t = Regex.Replace(t, @"^(eu\s+)?(preciso|quero|gostaria|necessito)\s+(de\s+|que\s+)?", "", RegexOptions.IgnoreCase);
        t = t.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? t;
        t = Regex.Replace(t, @"\s+(da|na|do|no)\s+minha\s+\w+$", "", RegexOptions.IgnoreCase);
        if (t.Length > 60)
        {
            var corte = t.LastIndexOf(' ', 57);
            t = t[..(corte > 20 ? corte : 57)] + "…";
        }
        return Capitalizar(t);
    }

    public static string Capitalizar(string t) =>
        string.IsNullOrEmpty(t) ? t : char.ToUpper(t[0], PtBr) + t[1..];

    public static string Moeda(decimal valor) => valor.ToString("C2", PtBr);

    public static string NumeroPedido(int id) => $"#{id:D4}";

    public static List<string> Linhas(string? texto) =>
        (texto ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    /// <summary>Procura a linha "Local: ..." nos requisitos do briefing.</summary>
    public static string? Local(string? requisitos)
    {
        var linha = Linhas(requisitos).FirstOrDefault(l => l.StartsWith("Local", StringComparison.OrdinalIgnoreCase));
        if (linha is null) return null;
        var i = linha.IndexOf(':');
        return i >= 0 ? linha[(i + 1)..].Trim() : linha;
    }

    /// <summary>Versão curta do local para os cards de convite: "Rua X, 120 — Vila Mariana, São Paulo/SP" → "Vila Mariana, São Paulo/SP".</summary>
    public static string? LocalCurto(string? requisitos)
    {
        var local = Local(requisitos);
        if (local is null) return null;
        var i = local.IndexOf('—');
        return i >= 0 ? local[(i + 1)..].Trim() : local;
    }

    public static string SubtituloTrabalhador(string? profissao, string? cidade) =>
        string.Join(" · ", new[] { profissao, cidade }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
