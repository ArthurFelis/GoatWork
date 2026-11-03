namespace GoatWork.Domain.Entities;

public class AceiteTermos
{
    public int IdAceite { get; set; }
    public int IdUsuario { get; set; }
    public int IdTermo { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
    public string Ip { get; set; } = string.Empty;

    public Usuario Usuario { get; set; } = null!;
    public TermoUso Termo { get; set; } = null!;
}
