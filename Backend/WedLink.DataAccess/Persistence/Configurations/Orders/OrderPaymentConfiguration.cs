using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Orders;

namespace WedLink.DataAccess.Persistence.Configurations.Orders;

public sealed class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        builder.ToTable("OrderPayments", table => table.HasCheckConstraint(
            "CK_OrderPayments_Amount_Positive", "[Amount] > 0"));

        builder.HasKey(payment => payment.OrderPaymentId);
        builder.Property(payment => payment.Amount).HasPrecision(10, 2);
        builder.Property(payment => payment.Method).HasMaxLength(40).IsRequired();
        builder.Property(payment => payment.Reference).HasMaxLength(200);
        builder.Property(payment => payment.Note).HasMaxLength(1000);
        builder.Property(payment => payment.RecordedByAdminUserId).HasMaxLength(450).IsRequired();
        builder.Property(payment => payment.RecordedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(payment => new { payment.OrderId, payment.RecordedAt });

        builder.HasOne(payment => payment.Order)
            .WithMany()
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(payment => payment.RecordedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
