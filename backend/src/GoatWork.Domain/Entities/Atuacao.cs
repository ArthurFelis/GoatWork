namespace GoatWork.Domain.Entities;

public class Atuacao
{
    public int IdTrabalhador { get; set; }
    public int IdCategoria { get; set; }
    public byte AnosExperiencia { get; set; }

    public Trabalhador Trabalhador { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
}
