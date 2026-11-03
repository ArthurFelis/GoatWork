namespace GoatWork.Application.Interfaces;

public interface IPasswordHasher
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);
}
