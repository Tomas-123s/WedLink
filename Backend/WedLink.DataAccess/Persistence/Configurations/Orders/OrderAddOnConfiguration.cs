using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class OrderAddOnConfiguration : IEntityTypeConfiguration<OrderAddOn>
{
    public void Configure(EntityTypeBuilder<OrderAddOn> builder)
    {
        builder.ToTable("OrderAddOns", table => table.HasCheckConstraint(
            "CK_OrderAddOns_PriceAtPurchase_NonNegative",
            "[PriceAtPurchase] >= 0"));

        builder.HasKey(item => item.OrderAddOnId);

        builder.Property(item => item.NameAtPurchase)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(item => item.PriceAtPurchase)
            .HasPrecision(10, 2);

        builder.HasIndex(item => new { item.OrderId, item.AddOnId })
            .IsUnique();

        builder.HasOne(item => item.Order)
            .WithMany()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AddOn>()
            .WithMany()
            .HasForeignKey(item => item.AddOnId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
