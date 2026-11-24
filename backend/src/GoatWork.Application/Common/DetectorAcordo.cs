using System.Globalization;
using System.Text.RegularExpressions;

namespace GoatWork.Application.Common;

/// <summary>
/// Lê as mensagens do chat e identifica o acordo (valor + data de entrega) para o card
/// "Acordo identificado" → "Gerar contrato".
/// </summary>
public static class DetectorAcordo
{
    private static readonly Regex Valor =
        new(@"R\$\s*(\d{1,3}(?:\.\d{3})+|\d+)(?:,(\d{1,2}))?(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Data =
        new(@"\b(\d{1,2})/(\d{1,2})(?:/(\d{2,4}))?\b", RegexOptions.Compiled);

    private static readonly Regex Hora =
        new(@"\b(?:às|as|a partir das)\s*(\d{1,2})(?:[:h](\d{2}))?\s*h?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Só consideramos acordo quando alguém confirma na conversa
    private static readonly Regex Confirmacao =
        new(@"\b(combinado|fechado|fechamos|fechou|de acordo|aceito|topo|pode ser|está ótimo|esta otimo|ok, pode)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public record Acordo(decimal Valor, DateOnly Data, string? Hora);

    public record Sugestao(decimal? Valor, DateOnly? Data, string? Hora);

    /// <summary>
    /// Percorre as mensagens da mais nova para a mais antiga e usa o último valor e a última data citados.
    /// </summary>
    public static Acordo? Detectar(IEnumerable<string> mensagensEmOrdem, DateOnly hoje, DateOnly? prazoBriefing = null)
    {
        var lista = mensagensEmOrdem.ToList();
        if (!lista.Any(m => Confirmacao.IsMatch(m))) return null;

        decimal? valor = null;
        DateOnly? data = null;
        string? hora = null;

        foreach (var msg in Enumerable.Reverse(lista))
        {
            valor ??= ExtrairValor(msg);
            data ??= ExtrairData(msg, hoje, prazoBriefing);
            hora ??= ExtrairHora(msg);
            if (valor is not null && data is not null) break;
        }

        // Se ninguém citou data no chat, usa o prazo desejado do briefing
        data ??= prazoBriefing;

        return valor is > 0 && data is not null ? new Acordo(valor.Value, data.Value, hora) : null;
    }

    /// <summary>
    /// Sugestão para pré-preencher o formulário "Enviar orçamento": último valor, data e hora citados
    /// no chat (sem exigir confirmação), com o prazo do briefing como reserva para a data.
    /// </summary>
    public static Sugestao Sugerir(IEnumerable<string> mensagensEmOrdem, DateOnly hoje, DateOnly? prazoBriefing, decimal? orcamentoBriefing)
    {
        decimal? valor = null;
        DateOnly? data = null;
        string? hora = null;
        foreach (var msg in Enumerable.Reverse(mensagensEmOrdem.ToList()))
        {
            valor ??= ExtrairValor(msg);
            data ??= ExtrairData(msg, hoje, prazoBriefing);
            hora ??= ExtrairHora(msg);
        }
        return new Sugestao(valor ?? orcamentoBriefing, data ?? prazoBriefing, hora);
    }

    public static decimal? ExtrairValor(string texto)
    {
        var m = Valor.Matches(texto).LastOrDefault();
        if (m is null) return null;
        var inteiro = m.Groups[1].Value.Replace(".", "");
        var centavos = m.Groups[2].Success ? m.Groups[2].Value.PadRight(2, '0') : "00";
        return decimal.TryParse($"{inteiro}.{centavos}", NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static DateOnly? ExtrairData(string texto, DateOnly hoje, DateOnly? referencia = null)
    {
        var m = Data.Matches(texto).LastOrDefault();
        if (m is null) return null;
        var dia = int.Parse(m.Groups[1].Value);
        var mes = int.Parse(m.Groups[2].Value);
        int ano;
        if (m.Groups[3].Success)
        {
            ano = int.Parse(m.Groups[3].Value);
            if (ano < 100) ano += 2000;
        }
        else
        {
            ano = referencia?.Year ?? hoje.Year;
        }
        if (mes is < 1 or > 12 || dia < 1 || dia > DateTime.DaysInMonth(ano, mes)) return null;
        var data = new DateOnly(ano, mes, dia);
        // "10/10" sem ano e já passou → próximo ano
        if (!m.Groups[3].Success && referencia is null && data < hoje) data = data.AddYears(1);
        return data;
    }

    public static string? ExtrairHora(string texto)
    {
        var m = Hora.Match(texto);
        if (!m.Success) return null;
        var h = int.Parse(m.Groups[1].Value);
        if (h > 23) return null;
        return m.Groups[2].Success ? $"{h}h{m.Groups[2].Value}" : $"{h}h";
    }
}
