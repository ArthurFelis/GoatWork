using System.Text;
using GoatWork.Api.Middlewares;
using GoatWork.Application.Common;
using GoatWork.Application.Interfaces;
using GoatWork.Application.Services;
using GoatWork.Infrastructure;
using GoatWork.Infrastructure.Data;
using GoatWork.Infrastructure.ExternalServices.IA;
using GoatWork.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ------------------------------------------------------------------ Configuração
var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Configure Jwt:Key no appsettings com pelo menos 32 caracteres.");
var ia = config.GetSection("IA").Get<IaOptions>() ?? new IaOptions();
var plataforma = config.GetSection("Plataforma").Get<PlataformaOptions>() ?? new PlataformaOptions();
var connectionString = config.GetConnectionString("GoatWork")
                       ?? throw new InvalidOperationException("Configure ConnectionStrings:GoatWork.");

// ------------------------------------------------------------------ Serviços
builder.Services.AddInfrastructure(connectionString, jwt, ia);
builder.Services.AddSingleton(plataforma);
builder.Services.AddSingleton<FiltroContatoExterno>();
builder.Services.AddScoped<TermoService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<TrabalhadorService>();
builder.Services.AddScoped<BriefingService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ConviteService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ContratoService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<EntregaService>();
builder.Services.AddScoped<DisputaService>();
builder.Services.AddScoped<AvaliacaoService>();
builder.Services.AddScoped<AdminService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Issuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
        // 401/403 em JSON, no mesmo formato do ErrorHandlingMiddleware
        opt.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { mensagem = "Sua sessão expirou. Entre novamente." });
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(new { mensagem = "Você não tem acesso a este recurso." });
            }
        };
    });
builder.Services.AddAuthorization();

var origens = config.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ------------------------------------------------------------------ Dados iniciais
if (config.GetValue("Seed:Executar", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

// ------------------------------------------------------------------ Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// O front-end (HTML/CSS/JS puro) é servido pela própria API: http://localhost:5080
var caminhoFront = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, config["Frontend:Caminho"] ?? "../../../frontend"));
if (Directory.Exists(caminhoFront))
{
    var arquivos = new PhysicalFileProvider(caminhoFront);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = arquivos });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = arquivos });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
