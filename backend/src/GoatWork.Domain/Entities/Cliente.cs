using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Cliente
{
    public int IdUsuario { get; set; }
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.FISICA;
    public string? NomeExibicao { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
