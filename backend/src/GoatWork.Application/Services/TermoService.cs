using GoatWork.Application.DTOs;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Services;

public class TermoService
{
    private readonly IAppDbContext _db;

    public TermoService(IAppDbContext db) => _db = db;

    public async Task<TermoUso> ObterVigenteEntidadeAsync() =>
        await _db.TermosUso.OrderByDescending(t => t.DataPublicacao).ThenByDescending(t => t.IdTermo).FirstOrDefaultAsync()
        ?? throw new NaoEncontradoException("Nenhuma versão dos Termos de Uso foi publicada.");

    public async Task<TermoDto> ObterVigenteAsync() => ParaDto(await ObterVigenteEntidadeAsync());

    public async Task<List<TermoDto>> ListarAsync() =>
        (await _db.TermosUso.OrderByDescending(t => t.DataPublicacao).ToListAsync()).Select(ParaDto).ToList();

    public async Task<TermoDto> PublicarAsync(PublicarTermoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Versao) || string.IsNullOrWhiteSpace(req.Texto))
            throw new RegraNegocioException("Informe a versão e o texto dos termos.");
        if (await _db.TermosUso.AnyAsync(t => t.Versao == req.Versao.Trim()))
            throw new RegraNegocioException("Já existe uma versão com esse número.");

        var termo = new TermoUso { Versao = req.Versao.Trim(), Texto = req.Texto.Trim(), DataPublicacao = DateTime.Now };
        _db.TermosUso.Add(termo);
        await _db.SaveChangesAsync();
        return ParaDto(termo);
    }

    /// <summary>Registra o aceite da versão vigente (com data, hora e IP), se ainda não existir.</summary>
    public async Task RegistrarAceiteAsync(Usuario usuario, string ip)
    {
        var termo = await ObterVigenteEntidadeAsync();
        var jaAceitou = usuario.IdUsuario != 0 &&
                        await _db.AceitesTermos.AnyAsync(a => a.IdUsuario == usuario.IdUsuario && a.IdTermo == termo.IdTermo);
        if (jaAceitou) return;

        _db.AceitesTermos.Add(new AceiteTermos
        {
            Usuario = usuario,
            IdTermo = termo.IdTermo,
            DataHora = DateTime.Now,
            Ip = string.IsNullOrWhiteSpace(ip) ? "desconhecido" : ip
        });
    }

    public static TermoDto ParaDto(TermoUso t) => new(
        t.IdTermo,
        t.Versao,
        t.DataPublicacao,
        t.Texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.StartsWith('!') ? new ItemTermoDto(l[1..].Trim(), true) : new ItemTermoDto(l, false))
            .ToList());
}
