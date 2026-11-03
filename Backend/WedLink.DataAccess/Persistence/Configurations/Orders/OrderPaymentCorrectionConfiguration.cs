using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Orders;

namespace WedLink.DataAccess.Persistence.Configurations.Orders;

public sealed class OrderPaymentCorrectionConfiguration : IEntityTypeConfiguration<OrderPaymentCorrection>
{
    public void Configure(EntityTypeBuilder<OrderPaymentCorrection> builder)
    {
        builder.ToTable("OrderPaymentCorrections", table =>
        {
            table.HasCheckConstraint("CK_OrderPaymentCorrections_PreviousAmount_Positive", "[PreviousAmount] > 0");
            table.HasCheckConstraint("CK_OrderPaymentCorrections_NewAmount_Positive", "[NewAmount] > 0");
        });
        builder.HasKey(item => item.OrderPaymentCorrectionId);
        builder.Property(item => item.PreviousAmount).HasPrecision(10, 2);
        builder.Property(item => item.NewAmount).HasPrecision(10, 2);
        builder.Property(item => item.PreviousMethod).HasMaxLength(40).IsRequired();
        builder.Property(item => item.NewMethod).HasMaxLength(40).IsRequired();
        builder.Property(item => item.PreviousReference).HasMaxLength(200);
        builder.Property(item => item.NewReference).HasMaxLength(200);
        builder.Property(item => item.PreviousNote).HasMaxLength(1000);
        builder.Property(item => item.NewNote).HasMaxLength(1000);
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.CorrectedByAdminUserId).HasMaxLength(450).IsRequired();
        builder.Property(item => item.CorrectedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(item => new { item.OrderPaymentId, item.CorrectedAt });
        builder.HasOne<OrderPayment>().WithMany().HasForeignKey(item => item.OrderPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdminUser>().WithMany().HasForeignKey(item => item.CorrectedByAdminUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
