namespace GoatWork.Application.Common;

/// <summary>Seção "Plataforma" do appsettings.json.</summary>
public class PlataformaOptions
{
    public decimal TaxaPercentualPadrao { get; set; } = 10m;

    /// <summary>Valor mínimo de cada saque da carteira do trabalhador.</summary>
    public decimal SaqueMinimo { get; set; } = 10m;
}
