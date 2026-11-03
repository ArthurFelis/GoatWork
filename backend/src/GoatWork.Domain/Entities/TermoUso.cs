namespace GoatWork.Domain.Entities;

public class TermoUso
{
    public int IdTermo { get; set; }
    public string Versao { get; set; } = string.Empty;
    /// <summary>Um item por linha. Linhas iniciadas com "!" são o destaque (isenção de responsabilidade).</summary>
    public string Texto { get; set; } = string.Empty;
    public DateTime DataPublicacao { get; set; } = DateTime.Now;
}
