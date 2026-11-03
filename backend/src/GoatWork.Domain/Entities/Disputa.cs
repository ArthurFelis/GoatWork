using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Disputa
{
    public int IdDisputa { get; set; }
    public int IdContrato { get; set; }
    public int IdAbertor { get; set; }
    public int? IdAdmin { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public StatusDisputa Status { get; set; } = StatusDisputa.ABERTA;
    public string? Decisao { get; set; }
    public DateTime DataAbertura { get; set; } = DateTime.Now;
    public DateTime? DataResolucao { get; set; }

    public Contrato Contrato { get; set; } = null!;
    public Usuario Abertor { get; set; } = null!;
    public Usuario? Admin { get; set; }
}
