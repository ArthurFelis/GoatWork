using GoatWork.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoatWork.Application.Interfaces;

/// <summary>Abstração do banco usada pelos Services (implementada pelo AppDbContext na Infrastructure).</summary>
public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Cliente> Clientes { get; }
    DbSet<Trabalhador> Trabalhadores { get; }
    DbSet<TermoUso> TermosUso { get; }
    DbSet<AceiteTermos> AceitesTermos { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<Atuacao> Atuacoes { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<MensagemIa> MensagensIa { get; }
    DbSet<Briefing> Briefings { get; }
    DbSet<Convite> Convites { get; }
    DbSet<Chat> Chats { get; }
    DbSet<Mensagem> Mensagens { get; }
    DbSet<Contrato> Contratos { get; }
    DbSet<Pagamento> Pagamentos { get; }
    DbSet<Entrega> Entregas { get; }
    DbSet<Disputa> Disputas { get; }
    DbSet<Avaliacao> Avaliacoes { get; }
    DbSet<Saque> Saques { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
