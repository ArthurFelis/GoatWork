using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

/// <summary>Retirada do saldo do trabalhador (valores liberados dos contratos) via Pix.</summary>
public class Saque
{
    public int IdSaque { get; set; }
    public int IdTrabalhador { get; set; }
    public decimal Valor { get; set; }
    public TipoChavePix TipoChave { get; set; }
    public string ChavePix { get; set; } = string.Empty;
    public StatusSaque Status { get; set; } = StatusSaque.SOLICITADO;
    public string? IdTransacaoGateway { get; set; }
    public DateTime DataSolicitacao { get; set; } = DateTime.Now;
    public DateTime? DataConclusao { get; set; }

    public Trabalhador Trabalhador { get; set; } = null!;
}
