using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Identity;

namespace WedLink.DataAccess.Persistence.Configurations.Orders;

public sealed class CustomerSubmissionMediaConfiguration : IEntityTypeConfiguration<CustomerSubmissionMedia>
{
    public void Configure(EntityTypeBuilder<CustomerSubmissionMedia> builder)
    {
        builder.ToTable("CustomerSubmissionMedia", table => table.HasCheckConstraint("CK_CustomerSubmissionMedia_Size_NonNegative", "[SizeBytes] >= 0"));
        builder.HasKey(item => item.CustomerSubmissionMediaId);
        builder.Property(item => item.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(item => item.MediaKind).HasMaxLength(20).IsRequired();
        builder.Property(item => item.DeletedByAdminUserId).HasMaxLength(450);
        builder.HasIndex(item => new { item.OrderId, item.DeletedAt });
        builder.HasOne<Order>().WithMany().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdminUser>().WithMany().HasForeignKey(item => item.DeletedByAdminUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
