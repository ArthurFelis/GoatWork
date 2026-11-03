using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Entrega
{
    public int IdEntrega { get; set; }
    public int IdContrato { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataEntrega { get; set; } = DateTime.Now;
    public StatusEntrega Status { get; set; } = StatusEntrega.PENDENTE;

    public Contrato Contrato { get; set; } = null!;
}
