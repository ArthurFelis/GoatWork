using System.Text.RegularExpressions;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Enums;

namespace GoatWork.Application.Common;

/// <summary>
/// Atendimento por regras, usado quando nenhuma chave de IA está configurada
/// (ou quando a IA externa falha). Segue o roteiro da tela "Conversa com a IA":
/// 1) entende o pedido, 2) pergunta endereço e prazo, 3) monta o briefing.
/// </summary>
public static class AtendimentoIaRegras
{
    public const string Saudacao = "Olá, sou o assistente da GoatWork, que serviço você precisa?";
    public const string PerguntaLocalPrazo = "Entendi! Qual é o endereço do serviço e o prazo que você prefere?";
    public const string PerguntaLocalPrazoRemoto = "Entendi! O serviço pode ser feito remotamente ou tem um local? E qual o prazo que você prefere?";
    public const string Finalizacao = "Perfeito. Já tenho o que preciso para montar o resumo do seu pedido.";

    private static readonly Regex Endereco = new(
        @"\b(rua|r\.|av\.?|avenida|alameda|al\.|travessa|estrada|rodovia|pra[çc]a|largo|condom[ií]nio|bairro)\b[^.!?\n]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Remoto = new(@"\b(remot[oa]|online|on-line|à distância|a distância)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Valor = new(@"(R\$\s*[\d\.]+(,\d{2})?)|(\d+[\d\.]*(,\d{2})?\s*reais)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DiaSemana = new(@"\b(segunda|ter[çc]a|quarta|quinta|sexta|s[áa]bado|domingo|amanh[ãa]|hoje|semana que vem|pr[óo]xima semana|urgente)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] CategoriasRemotas = { "Digital", "Aulas" };

    public static RespostaIa Responder(ContextoIa ctx)
    {
        var doCliente = ctx.Historico.Where(m => m.Remetente == RemetenteIa.CLIENTE).Select(m => m.Conteudo).ToList();
        var tudo = string.Join("\n", doCliente.Prepend(ctx.DescricaoInicial));
        var perguntasFeitas = ctx.Historico.Count(m => m.Remetente == RemetenteIa.IA && m.Conteudo.Contains('?'));

        var local = ExtrairLocal(tudo);
        var prazo = DetectorAcordo.ExtrairData(tudo, ctx.Hoje);
        var temPrazo = prazo is not null || DiaSemana.IsMatch(tudo);

        // Ainda falta local ou prazo e só fizemos a saudação → pergunta (uma vez)
        if ((local is null || !temPrazo) && perguntasFeitas <= 1)
        {
            var remota = CategoriasRemotas.Contains(ctx.Categoria);
            return new RespostaIa(remota ? PerguntaLocalPrazoRemoto : PerguntaLocalPrazo, null);
        }

        var orcamento = ExtrairOrcamento(tudo);
        var requisitos = new List<string> { "Executar o serviço com materiais e segurança adequados" };
        if (local is not null) requisitos.Add($"Local: {local}");
        requisitos.Add(local is not null && !Remoto.IsMatch(local)
            ? "Garantir acesso ao local no dia combinado"
            : "Manter a comunicação pelo chat da GoatWork");

        var briefing = new BriefingGerado(GerarResumo(ctx.DescricaoInicial), requisitos, prazo, orcamento);
        return new RespostaIa(Finalizacao, briefing);
    }

    public static string? ExtrairLocal(string texto)
    {
        var m = Endereco.Match(texto);
        if (m.Success)
        {
            // corta antes de datas/dias da semana: "Rua das Flores, 120, Vila Mariana. Sábado..."
            var local = Regex.Split(m.Value, @"\s+(no|na|dia|até|para)\s+(s[áa]bado|domingo|segunda|ter[çc]a|quarta|quinta|sexta|\d)", RegexOptions.IgnoreCase)[0];
            local = local.Trim().TrimEnd(',', '.', ';');
            return Texto.Capitalizar(local);
        }
        return Remoto.IsMatch(texto) ? "Remoto" : null;
    }

    public static decimal? ExtrairOrcamento(string texto)
    {
        var m = Valor.Match(texto);
        if (!m.Success) return null;
        var v = m.Value.Replace("R$", "").Replace("reais", "", StringComparison.OrdinalIgnoreCase).Trim();
        return DetectorAcordo.ExtrairValor("R$ " + v);
    }

    /// <summary>"Preciso instalar 4 tomadas e trocar o quadro de energia da minha casa." → "Instalar 4 tomadas e trocar o quadro de energia da minha casa."</summary>
    public static string GerarResumo(string descricao)
    {
        var t = Regex.Replace(descricao.Trim(), @"^(eu\s+)?(preciso|quero|gostaria|necessito)\s+(de\s+|que\s+)?", "", RegexOptions.IgnoreCase);
        t = Texto.Capitalizar(t.Trim());
        return t.EndsWith('.') ? t : t + ".";
    }
}
