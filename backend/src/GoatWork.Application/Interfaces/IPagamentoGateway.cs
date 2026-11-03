namespace GoatWork.Application.Interfaces;

/// <summary>Gateway de pagamento (Pix e cartão). A implementação padrão é simulada.</summary>
public interface IPagamentoGateway
{
    string GerarPixCopiaECola(string referencia, decimal valor);
    Task<ResultadoCobranca> CobrarAsync(CobrancaGateway cobranca, CancellationToken ct = default);
    Task<bool> LiberarAsync(string idTransacao, decimal valorLiquido, CancellationToken ct = default);
    Task<bool> EstornarAsync(string idTransacao, decimal valor, CancellationToken ct = default);
}

public record CobrancaGateway(string Referencia, string Metodo, decimal Valor, string? NumeroCartao, string? NomeCartao, string? Validade, string? Cvv);

public record ResultadoCobranca(bool Aprovado, string? IdTransacao, string? Mensagem);
