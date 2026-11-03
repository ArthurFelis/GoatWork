using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;

namespace GoatWork.Application.Common;

/// <summary>
/// Etapa atual de um contrato, usada na tabela "Pedidos e contratos" (Situação / Próximo passo).
/// </summary>
public static class Etapas
{
    public const string AguardandoAssinatura = "AGUARDANDO_ASSINATURA";
    public const string AguardandoPagamento = "AGUARDANDO_PAGAMENTO";
    public const string EmExecucao = "EM_EXECUCAO";
    public const string EntregaEnviada = "ENTREGA_ENVIADA";
    public const string EmDisputa = "EM_DISPUTA";
    public const string Concluido = "CONCLUIDO";
    public const string Cancelado = "CANCELADO";

    public static string De(Contrato c) => c.Status switch
    {
        StatusContrato.CANCELADO => Cancelado,
        StatusContrato.CONCLUIDO => Concluido,
        StatusContrato.EM_DISPUTA => EmDisputa,
        _ when c.AceiteClienteEm is null || c.AceiteTrabalhadorEm is null => AguardandoAssinatura,
        _ when c.Pagamento is null || c.Pagamento.Status is StatusPagamento.PENDENTE or StatusPagamento.FALHOU => AguardandoPagamento,
        _ when c.Entregas.Any(e => e.Status == StatusEntrega.PENDENTE) => EntregaEnviada,
        _ => EmExecucao
    };

    public static (decimal Taxa, decimal Liquido) Valores(decimal total, decimal percentual)
    {
        var taxa = Math.Round(total * percentual / 100m, 2, MidpointRounding.AwayFromZero);
        return (taxa, total - taxa);
    }
}
