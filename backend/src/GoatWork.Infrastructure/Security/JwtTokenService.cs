using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GoatWork.Application.Common;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace GoatWork.Infrastructure.Security;

/// <summary>Seção "Jwt" do appsettings.json.</summary>
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GoatWork";
    public int ExpiraEmMinutos { get; set; } = 120;
}

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opcoes;

    public JwtTokenService(JwtOptions opcoes) => _opcoes = opcoes;

    public string Gerar(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Tipo.ToString()),
            new("iniciais", Texto.Iniciais(usuario.Nome))
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Key));
        var token = new JwtSecurityToken(
            issuer: _opcoes.Issuer,
            audience: _opcoes.Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opcoes.ExpiraEmMinutos),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
