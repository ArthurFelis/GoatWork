using GoatWork.Application.Common;
using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly TermoService _termos;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens, TermoService termos)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _termos = termos;
    }

    public async Task<AuthResponse> CadastrarAsync(CadastroRequest req, string ip)
    {
        var tipo = LerTipo(req.Tipo);
        Validacao.Exigir(tipo != TipoUsuario.ADMIN, "Tipo de conta inválido.");
        Validacao.Exigir(req.AceitouTermos, "É preciso aceitar os termos de uso para criar a conta.");
        Validacao.Exigir(!string.IsNullOrWhiteSpace(req.Nome) && req.Nome.Trim().Length >= 3, "Informe seu nome completo.");
        Validacao.Exigir(Validacao.EmailValido(req.Email), "Informe um e-mail válido.");
        // Pessoa jurídica (cliente ou trabalhador) se cadastra com CNPJ; pessoa física, com CPF
        var juridica = string.Equals(req.TipoPessoa, "JURIDICA", StringComparison.OrdinalIgnoreCase);
        if (juridica) Validacao.Exigir(Validacao.CnpjValido(req.Cpf), "Informe um CNPJ válido.");
        else Validacao.Exigir(Validacao.CpfValido(req.Cpf), "Informe um CPF válido.");
        var problemaSenha = Validacao.ProblemaSenha(req.Senha);
        Validacao.Exigir(problemaSenha is null, problemaSenha ?? string.Empty);

        var email = req.Email.Trim().ToLowerInvariant();
        var cpf = Validacao.SoDigitos(req.Cpf);
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            throw new RegraNegocioException("Já existe uma conta com este e-mail.");
        if (await _db.Usuarios.AnyAsync(u => u.Cpf == cpf))
            throw new RegraNegocioException(juridica ? "Já existe uma conta com este CNPJ." : "Já existe uma conta com este CPF.");

        var usuario = new Usuario
        {
            Nome = req.Nome.Trim(),
            Email = email,
            SenhaHash = _hasher.Gerar(req.Senha),
            Cpf = cpf,
            Telefone = string.IsNullOrWhiteSpace(req.Telefone) ? null : req.Telefone.Trim(),
            Tipo = tipo,
            Status = StatusUsuario.ATIVO,
            DataCadastro = DateTime.Now
        };

        if (tipo == TipoUsuario.CLIENTE)
        {
            usuario.Cliente = new Cliente
            {
                TipoPessoa = juridica ? TipoPessoa.JURIDICA : TipoPessoa.FISICA,
                NomeExibicao = string.IsNullOrWhiteSpace(req.NomeExibicao) ? null : req.NomeExibicao.Trim()
            };
        }
        else
        {
            usuario.Trabalhador = new Trabalhador
            {
                Profissao = string.IsNullOrWhiteSpace(req.Profissao) ? null : req.Profissao.Trim(),
                Cidade = string.IsNullOrWhiteSpace(req.Cidade) ? null : req.Cidade.Trim(),
                TipoPessoa = juridica ? TipoPessoa.JURIDICA : TipoPessoa.FISICA,
                Verificado = false
            };
        }

        _db.Usuarios.Add(usuario);
        // Regra 1: o usuário só fica ativo com o aceite dos termos registrado
        await _termos.RegistrarAceiteAsync(usuario, ip);
        await _db.SaveChangesAsync();

        return new AuthResponse(_tokens.Gerar(usuario), Resumo(usuario));
    }

    public async Task<AuthResponse> EntrarAsync(LoginRequest req, string ip)
    {
        Validacao.Exigir(req.AceitouTermos, "É preciso aceitar os termos de uso para entrar.");
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.Include(u => u.Cliente).FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !_hasher.Verificar(req.Senha ?? string.Empty, usuario.SenhaHash))
            throw new RegraNegocioException("E-mail ou senha incorretos.");
        if (usuario.Status != StatusUsuario.ATIVO)
            throw new RegraNegocioException("Esta conta está " + usuario.Status.ToString().ToLowerInvariant() + ". Fale com o suporte.");

        var tipoSelecionado = LerTipo(req.Tipo);
        if (usuario.Tipo != TipoUsuario.ADMIN && usuario.Tipo != tipoSelecionado)
            throw new RegraNegocioException(usuario.Tipo == TipoUsuario.CLIENTE
                ? "Esta conta é de cliente. Selecione \"Sou Cliente\"."
                : "Esta conta é de trabalhador. Selecione \"Sou Trabalhador\".");

        await _termos.RegistrarAceiteAsync(usuario, ip);
        await _db.SaveChangesAsync();

        return new AuthResponse(_tokens.Gerar(usuario), Resumo(usuario));
    }

    public async Task<UsuarioResumoDto> ObterAsync(int idUsuario)
    {
        var u = await _db.Usuarios.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.IdUsuario == idUsuario)
                ?? throw new NaoEncontradoException("Usuário não encontrado.");
        return Resumo(u);
    }

    public static UsuarioResumoDto Resumo(Usuario u)
    {
        var nome = u.Cliente?.NomeExibicao ?? u.Nome;
        return new UsuarioResumoDto(u.IdUsuario, nome, u.Email, u.Tipo.ToString(), Texto.Iniciais(nome));
    }

    private static TipoUsuario LerTipo(string? tipo) =>
        Enum.TryParse<TipoUsuario>(tipo, true, out var t) ? t : throw new RegraNegocioException("Selecione \"Sou Cliente\" ou \"Sou Trabalhador\".");
}
