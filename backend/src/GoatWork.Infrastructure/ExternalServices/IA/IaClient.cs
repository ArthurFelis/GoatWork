using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoatWork.Application.Common;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoatWork.Infrastructure.ExternalServices.IA;

/// <summary>Seção "IA" do appsettings.json.</summary>
public class IaOptions
{
    /// <summary>Chave da API da Anthropic. Vazia = atendimento por regras (sem custo, funciona offline).</summary>
    public string ApiKey { get; set; } = string.Empty;
    public string Modelo { get; set; } = "claude-sonnet-5-5";
    public string Url { get; set; } = "https://api.anthropic.com/v1/messages";
    public int MaxTokens { get; set; } = 800;
}

/// <summary>
/// IA de atendimento. Com <see cref="IaOptions.ApiKey"/> configurada usa a API da Anthropic (Claude);
/// sem chave — ou se a chamada falhar — usa <see cref="AtendimentoIaRegras"/>.
/// </summary>
public class IaClient : IIaAtendimento
{
    private readonly HttpClient _http;
    private readonly IaOptions _opcoes;
    private readonly ILogger<IaClient> _logger;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public IaClient(HttpClient http, IaOptions opcoes, ILogger<IaClient> logger)
    {
        _http = http;
        _opcoes = opcoes;
        _logger = logger;
    }

    public async Task<RespostaIa> ProximaRespostaAsync(ContextoIa contexto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.ApiKey))
            return AtendimentoIaRegras.Responder(contexto);

        try
        {
            return await ChamarClaudeAsync(contexto, ct) ?? AtendimentoIaRegras.Responder(contexto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha na IA externa; usando atendimento por regras.");
            return AtendimentoIaRegras.Responder(contexto);
        }
    }

    private async Task<RespostaIa?> ChamarClaudeAsync(ContextoIa ctx, CancellationToken ct)
    {
        var system = $$$"""
            Você é o assistente de atendimento da GoatWork, plataforma que conecta clientes a trabalhadores autônomos no Brasil.
            Seu trabalho é entender o pedido do cliente e montar um briefing claro. Categoria escolhida: "{{{ctx.Categoria}}}".
            Hoje é {{{ctx.Hoje.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}}}.

            Regras:
            - Responda sempre em português do Brasil, de forma curta e cordial (1 ou 2 frases).
            - Faça no máximo uma pergunta por vez e no máximo 3 perguntas no total. Pergunte apenas o essencial: local (ou se é remoto) e prazo; valor só se o cliente quiser informar.
            - Nunca peça telefone, e-mail ou contatos externos. Toda a negociação acontece dentro da GoatWork.
            - Quando já tiver informação suficiente, encerre com: "Perfeito. Já tenho o que preciso para montar o resumo do seu pedido." e preencha o briefing.

            Responda SOMENTE com um JSON válido, sem texto fora dele, neste formato:
            {"resposta": "texto para o cliente", "briefing": null}
            ou, quando terminar:
            {"resposta": "texto para o cliente", "briefing": {"resumo": "frase objetiva do serviço, ex.: Instalação de 4 tomadas e troca do quadro de energia em residência.", "requisitos": ["Executar o serviço com materiais e segurança adequados", "Local: Rua X, 120 — Bairro, Cidade/UF", "Garantir acesso ao local no dia combinado"], "prazo_desejado": "AAAA-MM-DD ou null", "orcamento_estimado": 650.00 ou null}}
            Nos requisitos, use exatamente o prefixo "Local: " para o endereço (ou "Local: Remoto").
            """;

        var mensagens = new List<object>();
        foreach (var m in ctx.Historico)
        {
            var role = m.Remetente == RemetenteIa.CLIENTE ? "user" : "assistant";
            // A API exige alternância e começar com "user": juntamos mensagens seguidas do mesmo papel
            if (mensagens.Count == 0 && role == "assistant") continue;
            if (mensagens.Count > 0 && mensagens[^1] is Dictionary<string, string> ultima && ultima["role"] == role)
                ultima["content"] += "\n" + m.Conteudo;
            else
                mensagens.Add(new Dictionary<string, string> { ["role"] = role, ["content"] = m.Conteudo });
        }
        if (mensagens.Count == 0)
            mensagens.Add(new Dictionary<string, string> { ["role"] = "user", ["content"] = ctx.DescricaoInicial });

        using var req = new HttpRequestMessage(HttpMethod.Post, _opcoes.Url);
        req.Headers.Add("x-api-key", _opcoes.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = _opcoes.Modelo,
            max_tokens = _opcoes.MaxTokens,
            system,
            messages = mensagens
        }), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("IA respondeu {Status}: {Corpo}", (int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));
            return null;
        }

        var corpo = await resp.Content.ReadFromJsonAsync<RespostaApi>(Json, ct);
        var texto = corpo?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
        if (string.IsNullOrWhiteSpace(texto)) return null;

        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        if (inicio < 0 || fim <= inicio) return new RespostaIa(texto.Trim(), null);

        var json = JsonSerializer.Deserialize<RespostaJson>(texto[inicio..(fim + 1)], Json);
        if (json is null || string.IsNullOrWhiteSpace(json.Resposta)) return null;

        BriefingGerado? briefing = null;
        if (json.Briefing is { } b && !string.IsNullOrWhiteSpace(b.Resumo))
        {
            DateOnly? prazo = DateOnly.TryParseExact(b.PrazoDesejado, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
            briefing = new BriefingGerado(b.Resumo.Trim(), b.Requisitos?.Where(r => !string.IsNullOrWhiteSpace(r)).ToList() ?? new List<string>(), prazo, b.OrcamentoEstimado);
        }
        return new RespostaIa(json.Resposta.Trim(), briefing);
    }

    private sealed record RespostaApi(List<BlocoConteudo>? Content);

    private sealed record BlocoConteudo(string Type, string? Text);

    private sealed record RespostaJson(string Resposta, BriefingJson? Briefing);

    private sealed record BriefingJson(
        string Resumo,
        List<string>? Requisitos,
        [property: JsonPropertyName("prazo_desejado")] string? PrazoDesejado,
        [property: JsonPropertyName("orcamento_estimado")] decimal? OrcamentoEstimado);
}
