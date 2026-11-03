using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Customers;

namespace WedLink.DataAccess.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.CustomerId);

        builder.Property(customer => customer.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(customer => customer.Phone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(254);

        builder.Property(customer => customer.Notes)
            .HasMaxLength(1000);

        builder.Property(customer => customer.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(customer => customer.Phone)
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasQueryFilter(customer => customer.DeletedAt == null);
    }
}
