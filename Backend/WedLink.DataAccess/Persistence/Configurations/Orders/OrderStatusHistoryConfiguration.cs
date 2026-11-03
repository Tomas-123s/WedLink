using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Orders;

namespace WedLink.DataAccess.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasKey(history => history.OrderStatusHistoryId);

        builder.Property(history => history.PreviousStatus)
            .HasMaxLength(30);

        builder.Property(history => history.NewStatus)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(history => history.ChangedByAdminUserId)
            .IsRequired();

        builder.Property(history => history.ChangedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(history => history.Note)
            .HasMaxLength(1000);

        builder.HasIndex(history => new { history.OrderId, history.ChangedAt });

        builder.HasOne(item => item.Order)
            .WithMany()
            .HasForeignKey(history => history.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
