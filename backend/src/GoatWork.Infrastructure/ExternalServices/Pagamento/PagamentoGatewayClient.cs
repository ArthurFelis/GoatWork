using System.Text;
using GoatWork.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoatWork.Infrastructure.ExternalServices.Pagamento;

/// <summary>
/// Gateway SIMULADO (ambiente de desenvolvimento). Aprova Pix na hora e cartões válidos,
/// exceto o cartão de teste 4000 0000 0000 0002 (sempre recusado).
/// Para produção, troque por uma integração real (Mercado Pago, Pagar.me, Stripe...) mantendo a interface.
/// </summary>
public class PagamentoGatewayClient : IPagamentoGateway
{
    private const string CartaoRecusado = "4000000000000002";
    private readonly ILogger<PagamentoGatewayClient> _logger;

    public PagamentoGatewayClient(ILogger<PagamentoGatewayClient> logger) => _logger = logger;

    public string GerarPixCopiaECola(string referencia, decimal valor)
    {
        // Formato ilustrativo do BR Code (EMV). Um gateway real devolve este payload pronto.
        var valorTxt = valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var txid = new string(referencia.Where(char.IsLetterOrDigit).ToArray());
        var sb = new StringBuilder("00020126580014br.gov.bcb.pix0136goatwork-custodia-");
        sb.Append(txid.ToLowerInvariant()).Append("5204000053039865406").Append(valorTxt)
          .Append("5802BR5908GOATWORK6009SAO PAULO62").Append((txid.Length + 4).ToString("00"))
          .Append("05").Append(txid.Length.ToString("00")).Append(txid).Append("6304");
        return sb.ToString();
    }

    public Task<ResultadoCobranca> CobrarAsync(CobrancaGateway cobranca, CancellationToken ct = default)
    {
        var numero = new string((cobranca.NumeroCartao ?? "").Where(char.IsDigit).ToArray());
        if (cobranca.Metodo == "CARTAO" && numero == CartaoRecusado)
        {
            _logger.LogInformation("Gateway simulado: cartão recusado para {Ref}", cobranca.Referencia);
            return Task.FromResult(new ResultadoCobranca(false, null, "Pagamento recusado pela operadora do cartão."));
        }

        var id = $"sim_{cobranca.Metodo.ToLowerInvariant()}_{Guid.NewGuid():N}"[..32];
        _logger.LogInformation("Gateway simulado: {Metodo} aprovado {Valor} ({Ref}) -> {Id}", cobranca.Metodo, cobranca.Valor, cobranca.Referencia, id);
        return Task.FromResult(new ResultadoCobranca(true, id, null));
    }

    public Task<bool> LiberarAsync(string idTransacao, decimal valorLiquido, CancellationToken ct = default)
    {
        _logger.LogInformation("Gateway simulado: repasse de {Valor} da transação {Id}", valorLiquido, idTransacao);
        return Task.FromResult(true);
    }

    public Task<ResultadoCobranca> TransferirPixAsync(string referencia, decimal valor, string tipoChave, string chave, CancellationToken ct = default)
    {
        // Simulação: a chave de teste "falha@goatwork.com" sempre é recusada
        if (string.Equals(chave, "falha@goatwork.com", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new ResultadoCobranca(false, null, "A instituição do recebedor recusou a transferência. Confira a chave Pix."));

        var id = $"sim_saque_{Guid.NewGuid():N}"[..32];
        _logger.LogInformation("Gateway simulado: saque Pix de {Valor} ({Tipo}) {Ref} -> {Id}", valor, tipoChave, referencia, id);
        return Task.FromResult(new ResultadoCobranca(true, id, null));
    }

    public Task<bool> EstornarAsync(string idTransacao, decimal valor, CancellationToken ct = default)
    {
        _logger.LogInformation("Gateway simulado: estorno de {Valor} da transação {Id}", valor, idTransacao);
        return Task.FromResult(true);
    }
}
