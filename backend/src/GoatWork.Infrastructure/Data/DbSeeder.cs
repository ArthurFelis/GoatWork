using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using GoatWork.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Infrastructure.Data;

/// <summary>
/// Dados iniciais para desenvolvimento: categorias, Termos de Uso v1.0, administrador,
/// um cliente e os profissionais que aparecem nas telas do Figma.
/// Senha de todas as contas de exemplo: <see cref="SenhaPadrao"/>.
/// </summary>
public static class DbSeeder
{
    public const string SenhaPadrao = "Goat@1234";

    public static readonly string[] Categorias = { "Casa e reparos", "Limpeza", "Digital", "Eventos", "Aulas", "Beleza" };

    public const string TextoTermosV1 =
        "A GoatWork é uma plataforma de intermediação entre clientes e trabalhadores autônomos, não sendo empregadora nem prestadora dos serviços contratados.\n" +
        "!A GoatWork não se responsabiliza por danos materiais, físicos ou morais causados durante a execução do trabalho, sendo a responsabilidade exclusiva das partes envolvidas.\n" +
        "Cliente e trabalhador comprometem-se a agir de boa-fé e a cumprir o contrato digital gerado na plataforma.\n" +
        "Todo pagamento deve ser feito pela própria plataforma. Pagamentos por fora não terão qualquer proteção.\n" +
        "As conversas no chat ficam registradas e podem ser usadas para mediação em caso de disputa.";

    private record Profissional(string Nome, string Email, string Cpf, string Profissao, string Cidade, decimal Valor, string Unidade,
        decimal Nota, int Servicos, string Bio, (string Categoria, byte Anos)[] Atuacoes);

    private static readonly Profissional[] Profissionais =
    {
        new("Carlos Mendes", "carlos@goatwork.com", "29141777638", "Eletricista", "São Paulo, SP", 90, "h", 4.92m, 312,
            "Instalações residenciais, quadros de energia e manutenção preventiva.", new[] { ("Casa e reparos", (byte)12), ("Aulas", (byte)3) }),
        new("Ana Souza", "ana@goatwork.com", "01152449303", "Diarista", "Campinas, SP", 180, "dia", 4.81m, 540,
            "Limpeza pesada, pós-obra e organização de ambientes.", new[] { ("Limpeza", (byte)8) }),
        new("Rafael Lima", "rafael@goatwork.com", "39825979194", "Designer gráfico", "Remoto", 120, "h", 4.80m, 127,
            "Identidade visual, logos e materiais para redes sociais.", new[] { ("Digital", (byte)6) }),
        new("Juliana Costa", "juliana@goatwork.com", "34167211017", "Fotógrafa", "Rio de Janeiro, RJ", 800, "evento", 4.90m, 210,
            "Casamentos, aniversários e ensaios corporativos.", new[] { ("Eventos", (byte)9) }),
        new("Marcos Pereira", "marcos@goatwork.com", "94580730224", "Encanador", "Belo Horizonte, MG", 85, "h", 4.70m, 289,
            "Vazamentos, desentupimentos e instalação hidráulica.", new[] { ("Casa e reparos", (byte)15) }),
        new("Beatriz Alves", "beatriz@goatwork.com", "91740029704", "Professora de inglês", "Remoto", 70, "h", 4.88m, 403,
            "Aulas para conversação, negócios e preparação para provas.", new[] { ("Aulas", (byte)10) }),
        new("Diego Rocha", "diego@goatwork.com", "52601815906", "Desenvolvedor web", "Remoto", 150, "h", 4.78m, 98,
            "Sites, lojas virtuais e sistemas sob medida.", new[] { ("Digital", (byte)7) }),
        new("Patricia Nunes", "patricia@goatwork.com", "35623012353", "Maquiadora", "Curitiba, PR", 250, "sessão", 4.79m, 176,
            "Maquiagem social, noivas e produções fotográficas.", new[] { ("Beleza", (byte)11) }),
    };

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        // Categorias e termos (também estão em database/scripts/02-seed.sql)
        foreach (var nome in Categorias)
            if (!await db.Categorias.AnyAsync(c => c.Nome == nome))
                db.Categorias.Add(new Categoria { Nome = nome });

        if (!await db.TermosUso.AnyAsync())
            db.TermosUso.Add(new TermoUso { Versao = "1.0", Texto = TextoTermosV1, DataPublicacao = DateTime.Now });

        await db.SaveChangesAsync();

        if (await db.Usuarios.AnyAsync()) return; // só popula um banco vazio

        var termo = await db.TermosUso.OrderBy(t => t.IdTermo).FirstAsync();
        var categorias = await db.Categorias.ToDictionaryAsync(c => c.Nome);
        var hash = hasher.Gerar(SenhaPadrao);

        Usuario NovoUsuario(string nome, string email, string cpf, TipoUsuario tipo)
        {
            var u = new Usuario { Nome = nome, Email = email, Cpf = cpf, SenhaHash = hash, Tipo = tipo, Status = StatusUsuario.ATIVO, DataCadastro = DateTime.Now };
            u.Aceites.Add(new AceiteTermos { IdTermo = termo.IdTermo, DataHora = DateTime.Now, Ip = "seed" });
            return u;
        }

        db.Usuarios.Add(NovoUsuario("Administrador", "admin@goatwork.com", "66048764707", TipoUsuario.ADMIN));

        var gabriel = NovoUsuario("Gabriel Oliveira", "gabriel@goatwork.com", "79542205830", TipoUsuario.CLIENTE);
        gabriel.Telefone = "(11) 90000-0000";
        gabriel.Cliente = new Cliente { TipoPessoa = TipoPessoa.FISICA };
        db.Usuarios.Add(gabriel);
        await db.SaveChangesAsync();

        foreach (var p in Profissionais)
        {
            var u = NovoUsuario(p.Nome, p.Email, p.Cpf, TipoUsuario.TRABALHADOR);
            u.Trabalhador = new Trabalhador
            {
                Biografia = p.Bio,
                Profissao = p.Profissao,
                Cidade = p.Cidade,
                ValorReferencia = p.Valor,
                UnidadeValor = p.Unidade,
                NotaMedia = p.Nota,
                ServicosConcluidos = p.Servicos,
                TotalAvaliacoes = p.Servicos,
                Verificado = true,
                DataVerificacao = new DateTime(2026, 8, 12)
            };
            foreach (var (categoria, anos) in p.Atuacoes)
                u.Trabalhador.Atuacoes.Add(new Atuacao { IdCategoria = categorias[categoria].IdCategoria, AnosExperiencia = anos });
            db.Usuarios.Add(u);
            await db.SaveChangesAsync(); // um por vez: mantém a ordem de cadastro (ids crescentes)
        }
    }
}
