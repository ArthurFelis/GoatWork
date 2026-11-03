namespace GoatWork.Domain.Entities;

public class Briefing
{
    public int IdBriefing { get; set; }
    public int IdPedido { get; set; }
    public string Resumo { get; set; } = string.Empty;
    /// <summary>Um requisito por linha.</summary>
    public string? Requisitos { get; set; }
    public DateOnly? PrazoDesejado { get; set; }
    public decimal? OrcamentoEstimado { get; set; }
    public bool ConfirmadoCliente { get; set; }
    public DateTime? DataConfirmacao { get; set; }

    public Pedido Pedido { get; set; } = null!;
}
