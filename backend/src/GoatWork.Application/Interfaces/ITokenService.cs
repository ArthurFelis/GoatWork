using GoatWork.Domain.Entities;

namespace GoatWork.Application.Interfaces;

public interface ITokenService
{
    string Gerar(Usuario usuario);
}
