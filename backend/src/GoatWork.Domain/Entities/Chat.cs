using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Chat
{
    public int IdChat { get; set; }
    public int IdConvite { get; set; }
    public StatusChat Status { get; set; } = StatusChat.ABERTO;
    public DateTime DataAbertura { get; set; } = DateTime.Now;

    public Convite Convite { get; set; } = null!;
    public Contrato? Contrato { get; set; }
    public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
}
