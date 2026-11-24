using GoatWork.Domain.Enums;

namespace GoatWork.Domain.Entities;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    /// <summary>CPF (11 dígitos) ou, para cliente pessoa jurídica, CNPJ (14 dígitos).</summary>
    public string Cpf { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public TipoUsuario Tipo { get; set; }
    public StatusUsuario Status { get; set; } = StatusUsuario.ATIVO;
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public Cliente? Cliente { get; set; }
    public Trabalhador? Trabalhador { get; set; }
    public ICollection<AceiteTermos> Aceites { get; set; } = new List<AceiteTermos>();
}
