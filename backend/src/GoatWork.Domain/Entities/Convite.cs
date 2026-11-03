using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Convite
{
    public int IdConvite { get; set; }
    public int IdPedido { get; set; }
    public int IdTrabalhador { get; set; }
    public StatusConvite Status { get; set; } = StatusConvite.ENVIADO;
    public DateTime DataEnvio { get; set; } = DateTime.Now;
    public DateTime? DataResposta { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Trabalhador Trabalhador { get; set; } = null!;
    public Chat? Chat { get; set; }
}
