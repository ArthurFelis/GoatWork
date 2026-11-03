using System.Text.RegularExpressions;

namespace GoatWork.Application.Services;

// RF13: detecta telefone/e-mail/links em mensagens do chat.
// A mensagem continua registrada (para mediação de disputas), mas é marcada como bloqueada
// e não é entregue para a outra parte.
public class FiltroContatoExterno
{
    private static readonly Regex Email =
        new(@"[A-Z0-9._%+-]+\s*(@|\(at\)|\[at\])\s*[A-Z0-9.-]+\s*(\.|\(dot\))\s*[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Link =
        new(@"(https?://|www\.)\S+|\b\S+\.(com|net|org|br|me|io|app|link)(/\S*)?\b|wa\.me|t\.me", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 8 ou mais dígitos com separadores comuns de telefone (espaço, hífen, ponto, parênteses, +)
    private static readonly Regex Telefone =
        new(@"(\+?\d[\s\-\.\(\)]*){8,}", RegexOptions.Compiled);

    private static readonly Regex AppsExternos =
        new(@"\b(whats\s*app|whatsapp|whats|zap|wpp|zapzap|telegram|insta(gram)?|direct|signal|meu\s+(n[uú]mero|cel(ular)?|telefone|e-?mail|insta)|me\s+(liga|chama)\s+no)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Datas (10/10/2026) e valores (R$ 1.500,00) não podem ser confundidos com telefone
    private static readonly Regex DatasEValores =
        new(@"\b\d{1,2}/\d{1,2}(/\d{2,4})?\b|R\$\s*[\d\.]+(,\d{2})?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool ContemContatoExterno(string? conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo)) return false;
        var texto = DatasEValores.Replace(conteudo, " ");
        return Email.IsMatch(texto) || Link.IsMatch(texto) || Telefone.IsMatch(texto) || AppsExternos.IsMatch(texto);
    }

    /// <summary>Esconde os dígitos de uma mensagem bloqueada antes de exibi-la ao próprio remetente.</summary>
    public static string Mascarar(string conteudo) =>
        Regex.Replace(Email.Replace(conteudo, "•••@•••"), @"\d", "•");
}
