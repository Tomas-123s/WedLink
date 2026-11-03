using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table => table.HasCheckConstraint(
            "CK_Orders_AgreedBasePrice_NonNegative",
            "[AgreedBasePrice] >= 0"));

        builder.HasKey(order => order.OrderId);

        builder.Property(order => order.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.AgreedBasePrice)
            .HasPrecision(10, 2);

        builder.Property(order => order.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(order => order.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CreatedAt);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TemplateVersion>()
            .WithMany()
            .HasForeignKey(order => order.SelectedTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
