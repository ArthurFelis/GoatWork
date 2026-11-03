namespace GoatWork.Domain.Entities;

public class Mensagem
{
    public int IdMensagem { get; set; }
    public int IdChat { get; set; }
    public int IdRemetente { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public bool Bloqueada { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;

    public Chat Chat { get; set; } = null!;
    public Usuario Remetente { get; set; } = null!;
}
