using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Pagamento
{
    public int IdPagamento { get; set; }
    public int IdContrato { get; set; }
    public MetodoPagamento Metodo { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorTaxa { get; set; }
    public decimal ValorLiquido { get; set; }
    public StatusPagamento Status { get; set; } = StatusPagamento.PENDENTE;
    public string? IdTransacaoGateway { get; set; }
    public DateTime? DataPagamento { get; set; }
    public DateTime? DataLiberacao { get; set; }

    public Contrato Contrato { get; set; } = null!;
}
