using System.Text.RegularExpressions;
using GoatWork.Application.Interfaces;
using GoatWork.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Infrastructure.Data;

/// <summary>
/// Mapeamento "Database First" do script database/GoatWork.sql.
/// Tabelas e colunas em snake_case; ENUMs gravados como texto.
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Trabalhador> Trabalhadores => Set<Trabalhador>();
    public DbSet<TermoUso> TermosUso => Set<TermoUso>();
    public DbSet<AceiteTermos> AceitesTermos => Set<AceiteTermos>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Atuacao> Atuacoes => Set<Atuacao>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<MensagemIa> MensagensIa => Set<MensagemIa>();
    public DbSet<Briefing> Briefings => Set<Briefing>();
    public DbSet<Convite> Convites => Set<Convite>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<Entrega> Entregas => Set<Entrega>();
    public DbSet<Disputa> Disputas => Set<Disputa>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ---------------- Fase 1 ----------------
        mb.Entity<TermoUso>(e =>
        {
            e.ToTable("termo_uso");
            e.HasKey(x => x.IdTermo);
            e.Property(x => x.Versao).HasMaxLength(20);
            e.HasIndex(x => x.Versao).IsUnique();
        });

        mb.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(190);
            e.Property(x => x.Cpf).HasMaxLength(11).IsFixedLength();
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.Tipo).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Cpf).IsUnique();
        });

        mb.Entity<AceiteTermos>(e =>
        {
            e.ToTable("aceite_termos");
            e.HasKey(x => x.IdAceite);
            e.Property(x => x.Ip).HasMaxLength(45);
            e.HasIndex(x => new { x.IdUsuario, x.IdTermo }).IsUnique();
            e.HasOne(x => x.Usuario).WithMany(u => u.Aceites).HasForeignKey(x => x.IdUsuario);
            e.HasOne(x => x.Termo).WithMany().HasForeignKey(x => x.IdTermo);
        });

        mb.Entity<Trabalhador>(e =>
        {
            e.ToTable("trabalhador");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).ValueGeneratedNever();
            e.Property(x => x.NotaMedia).HasPrecision(3, 2);
            e.Property(x => x.Profissao).HasMaxLength(80);
            e.Property(x => x.Cidade).HasMaxLength(100);
            e.Property(x => x.ValorReferencia).HasPrecision(12, 2);
            e.Property(x => x.UnidadeValor).HasMaxLength(20);
            e.HasOne(x => x.Usuario).WithOne(u => u.Trabalhador).HasForeignKey<Trabalhador>(x => x.IdUsuario);
        });

        mb.Entity<Cliente>(e =>
        {
            e.ToTable("cliente");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).ValueGeneratedNever();
            e.Property(x => x.TipoPessoa).HasConversion<string>();
            e.Property(x => x.NomeExibicao).HasMaxLength(100);
            e.HasOne(x => x.Usuario).WithOne(u => u.Cliente).HasForeignKey<Cliente>(x => x.IdUsuario);
        });

        mb.Entity<Categoria>(e =>
        {
            e.ToTable("categoria");
            e.HasKey(x => x.IdCategoria);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.HasIndex(x => x.Nome).IsUnique();
        });

        mb.Entity<Atuacao>(e =>
        {
            e.ToTable("atuacao");
            e.HasKey(x => new { x.IdTrabalhador, x.IdCategoria });
            e.HasOne(x => x.Trabalhador).WithMany(t => t.Atuacoes).HasForeignKey(x => x.IdTrabalhador);
            e.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.IdCategoria);
        });

        // ---------------- Fase 2 ----------------
        mb.Entity<Pedido>(e =>
        {
            e.ToTable("pedido");
            e.HasKey(x => x.IdPedido);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Cliente).WithMany(c => c.Pedidos).HasForeignKey(x => x.IdCliente);
            e.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.IdCategoria);
            e.HasOne(x => x.TrabalhadorPreferido).WithMany().HasForeignKey(x => x.IdTrabalhadorPreferido).IsRequired(false);
        });

        mb.Entity<MensagemIa>(e =>
        {
            e.ToTable("mensagem_ia");
            e.HasKey(x => x.IdMsgIa);
            e.Property(x => x.Remetente).HasConversion<string>();
            e.HasOne(x => x.Pedido).WithMany(p => p.MensagensIa).HasForeignKey(x => x.IdPedido);
        });

        mb.Entity<Briefing>(e =>
        {
            e.ToTable("briefing");
            e.HasKey(x => x.IdBriefing);
            e.Property(x => x.OrcamentoEstimado).HasPrecision(12, 2);
            e.HasIndex(x => x.IdPedido).IsUnique();
            e.HasOne(x => x.Pedido).WithOne(p => p.Briefing).HasForeignKey<Briefing>(x => x.IdPedido);
        });

        // ---------------- Fase 3 ----------------
        mb.Entity<Convite>(e =>
        {
            e.ToTable("convite");
            e.HasKey(x => x.IdConvite);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => new { x.IdPedido, x.IdTrabalhador }).IsUnique();
            e.HasOne(x => x.Pedido).WithMany(p => p.Convites).HasForeignKey(x => x.IdPedido);
            e.HasOne(x => x.Trabalhador).WithMany(t => t.Convites).HasForeignKey(x => x.IdTrabalhador);
        });

        mb.Entity<Chat>(e =>
        {
            e.ToTable("chat");
            e.HasKey(x => x.IdChat);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.IdConvite).IsUnique();
            e.HasOne(x => x.Convite).WithOne(c => c.Chat).HasForeignKey<Chat>(x => x.IdConvite);
        });

        mb.Entity<Mensagem>(e =>
        {
            e.ToTable("mensagem");
            e.HasKey(x => x.IdMensagem);
            e.HasOne(x => x.Chat).WithMany(c => c.Mensagens).HasForeignKey(x => x.IdChat);
            e.HasOne(x => x.Remetente).WithMany().HasForeignKey(x => x.IdRemetente);
        });

        // ---------------- Fase 4 ----------------
        mb.Entity<Contrato>(e =>
        {
            e.ToTable("contrato");
            e.HasKey(x => x.IdContrato);
            e.Ignore(x => x.Numero);
            e.Property(x => x.ValorAcordado).HasPrecision(12, 2);
            e.Property(x => x.TaxaPlataformaPerc).HasPrecision(5, 2);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.IdChat).IsUnique();
            e.HasOne(x => x.Chat).WithOne(c => c.Contrato).HasForeignKey<Contrato>(x => x.IdChat);
        });

        mb.Entity<Pagamento>(e =>
        {
            e.ToTable("pagamento");
            e.HasKey(x => x.IdPagamento);
            e.Property(x => x.Metodo).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.ValorTotal).HasPrecision(12, 2);
            e.Property(x => x.ValorTaxa).HasPrecision(12, 2);
            e.Property(x => x.ValorLiquido).HasPrecision(12, 2);
            e.Property(x => x.IdTransacaoGateway).HasMaxLength(100);
            e.HasIndex(x => x.IdContrato).IsUnique();
            e.HasOne(x => x.Contrato).WithOne(c => c.Pagamento).HasForeignKey<Pagamento>(x => x.IdContrato);
        });

        mb.Entity<Entrega>(e =>
        {
            e.ToTable("entrega");
            e.HasKey(x => x.IdEntrega);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Contrato).WithMany(c => c.Entregas).HasForeignKey(x => x.IdContrato);
        });

        // ---------------- Fase 5 ----------------
        mb.Entity<Disputa>(e =>
        {
            e.ToTable("disputa");
            e.HasKey(x => x.IdDisputa);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Contrato).WithMany(c => c.Disputas).HasForeignKey(x => x.IdContrato);
            e.HasOne(x => x.Abertor).WithMany().HasForeignKey(x => x.IdAbertor);
            e.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.IdAdmin).IsRequired(false);
        });

        mb.Entity<Avaliacao>(e =>
        {
            e.ToTable("avaliacao");
            e.HasKey(x => x.IdAvaliacao);
            e.HasIndex(x => new { x.IdContrato, x.IdAvaliador }).IsUnique();
            e.HasOne(x => x.Contrato).WithMany(c => c.Avaliacoes).HasForeignKey(x => x.IdContrato);
            e.HasOne(x => x.Avaliador).WithMany().HasForeignKey(x => x.IdAvaliador);
            e.HasOne(x => x.Avaliado).WithMany().HasForeignKey(x => x.IdAvaliado);
        });

        // Nomes de colunas em snake_case (IdUsuario → id_usuario) e sem exclusão em cascata pelo EF
        foreach (var entidade in mb.Model.GetEntityTypes())
        {
            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(SnakeCase(propriedade.Name));
            foreach (var fk in entidade.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static string SnakeCase(string nome) =>
        Regex.Replace(nome, "(?<=[a-z0-9])([A-Z])", "_$1").ToLowerInvariant();
}
