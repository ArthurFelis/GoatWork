namespace GoatWork.Domain.Entities;

public class Avaliacao
{
    public int IdAvaliacao { get; set; }
    public int IdContrato { get; set; }
    public int IdAvaliador { get; set; }
    public int IdAvaliado { get; set; }
    public byte Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime Data { get; set; } = DateTime.Now;

    public Contrato Contrato { get; set; } = null!;
    public Usuario Avaliador { get; set; } = null!;
    public Usuario Avaliado { get; set; } = null!;
}
