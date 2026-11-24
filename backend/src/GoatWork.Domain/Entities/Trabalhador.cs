using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Trabalhador
{
    public int IdUsuario { get; set; }
    public string? Biografia { get; set; }
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.FISICA;
    public bool Verificado { get; set; }
    public decimal NotaMedia { get; set; }
    public DateTime? DataVerificacao { get; set; }

    // Dados exibidos nas telas "Encontre profissionais" e "Meu perfil"
    public string? Profissao { get; set; }
    public string? Cidade { get; set; }
    public decimal? ValorReferencia { get; set; }
    public string? UnidadeValor { get; set; }
    public int ServicosConcluidos { get; set; }
    public int TotalAvaliacoes { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Atuacao> Atuacoes { get; set; } = new List<Atuacao>();
    public ICollection<Convite> Convites { get; set; } = new List<Convite>();
}
