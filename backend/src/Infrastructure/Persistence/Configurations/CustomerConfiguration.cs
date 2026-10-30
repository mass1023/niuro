using Loans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loans.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(200);

        builder.Property(c => c.Ssn)
            .HasConversion(ssn => ssn.Value, value => Ssn.Create(value))
            .IsRequired()
            .HasMaxLength(9);

        builder.HasIndex(c => c.Ssn).IsUnique();

        builder.ComplexProperty(c => c.Address, address =>
        {
            address.Property(a => a.Street).IsRequired().HasMaxLength(200);
            address.Property(a => a.City).IsRequired().HasMaxLength(100);
            address.Property(a => a.State).IsRequired().HasMaxLength(2);
            address.Property(a => a.ZipCode).IsRequired().HasMaxLength(5);
        });
    }
}
