using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class MensagemIa
{
    public int IdMsgIa { get; set; }
    public int IdPedido { get; set; }
    public RemetenteIa Remetente { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.Now;

    public Pedido Pedido { get; set; } = null!;
}
