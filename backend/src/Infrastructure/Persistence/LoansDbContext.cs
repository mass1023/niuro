using Loans.Domain;
using Loans.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Loans.Infrastructure.Persistence;

public sealed class LoansDbContext(DbContextOptions<LoansDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LoansDbContext).Assembly);
    }
}
