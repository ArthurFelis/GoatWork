using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Pedido
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public int IdCategoria { get; set; }
    public string DescricaoInicial { get; set; } = string.Empty;
    public StatusPedido Status { get; set; } = StatusPedido.RASCUNHO;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    /// <summary>Preenchido quando o cliente clica em "Contratar" num profissional específico.</summary>
    public int? IdTrabalhadorPreferido { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
    public Trabalhador? TrabalhadorPreferido { get; set; }
    public Briefing? Briefing { get; set; }
    public ICollection<MensagemIa> MensagensIa { get; set; } = new List<MensagemIa>();
    public ICollection<Convite> Convites { get; set; } = new List<Convite>();
}
