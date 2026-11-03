using GoatWork.Application.Interfaces;
using GoatWork.Infrastructure.Data;
using GoatWork.Infrastructure.ExternalServices.IA;
using GoatWork.Infrastructure.ExternalServices.Pagamento;
using GoatWork.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoatWork.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra banco (MySQL via Pomelo), segurança, IA e gateway de pagamento.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString,
        JwtOptions jwt, IaOptions ia)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)),
                mysql => mysql.EnableRetryOnFailure(3)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton(jwt);
        services.AddSingleton(ia);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IPagamentoGateway, PagamentoGatewayClient>();
        services.AddHttpClient<IIaAtendimento, IaClient>(c => c.Timeout = TimeSpan.FromSeconds(30));

        return services;
    }
}
