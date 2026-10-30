using Loans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loans.Infrastructure.Persistence.Configurations;

internal sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("LoanApplications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.RequestedAmount).HasPrecision(18, 2);

        // One customer <-> one application: the FK side of a one-to-one gets a unique index.
        builder.HasOne<Customer>()
            .WithOne()
            .HasForeignKey<LoanApplication>(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
