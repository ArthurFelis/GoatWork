using GoatWork.Domain.Enums;

namespace GoatWork.Application.Interfaces;

/// <summary>IA de atendimento: conduz a triagem do pedido e monta o briefing.</summary>
public interface IIaAtendimento
{
    Task<RespostaIa> ProximaRespostaAsync(ContextoIa contexto, CancellationToken ct = default);
}

public record MensagemContextoIa(RemetenteIa Remetente, string Conteudo);

public record ContextoIa(string Categoria, string DescricaoInicial, IReadOnlyList<MensagemContextoIa> Historico, DateOnly Hoje);

public record BriefingGerado(string Resumo, IReadOnlyList<string> Requisitos, DateOnly? PrazoDesejado, decimal? OrcamentoEstimado);

/// <summary>Resposta da IA. Quando <see cref="Briefing"/> vem preenchido, a triagem terminou.</summary>
public record RespostaIa(string Mensagem, BriefingGerado? Briefing);
