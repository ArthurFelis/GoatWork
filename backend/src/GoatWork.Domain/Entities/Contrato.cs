using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Contrato
{
    public int IdContrato { get; set; }
    public int IdChat { get; set; }
    /// <summary>Cláusulas no formato "Título\nTexto", separadas por linha em branco.</summary>
    public string TextoContrato { get; set; } = string.Empty;
    public decimal ValorAcordado { get; set; }
    public DateOnly PrazoEntrega { get; set; }
    /// <summary>Horário combinado (ex.: "9h"), informado no orçamento do trabalhador.</summary>
    public string? HoraServico { get; set; }
    /// <summary>Local combinado (endereço ou "Remoto").</summary>
    public string? LocalServico { get; set; }
    public decimal TaxaPlataformaPerc { get; set; }
    public StatusContrato Status { get; set; } = StatusContrato.GERADO;
    public DateTime DataGeracao { get; set; } = DateTime.Now;
    public DateTime? AceiteClienteEm { get; set; }
    public DateTime? AceiteTrabalhadorEm { get; set; }

    public Chat Chat { get; set; } = null!;
    public Pagamento? Pagamento { get; set; }
    public ICollection<Entrega> Entregas { get; set; } = new List<Entrega>();
    public ICollection<Disputa> Disputas { get; set; } = new List<Disputa>();
    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();

    /// <summary>Número exibido nas telas: GW-2026-0481.</summary>
    public string Numero => $"GW-{DataGeracao.Year}-{IdContrato:D4}";
}
