using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Identity;

namespace WedLink.DataAccess.Persistence.Configurations.Orders;

public sealed class CustomerWeddingSubmissionConfiguration : IEntityTypeConfiguration<CustomerWeddingSubmission>
{
    public void Configure(EntityTypeBuilder<CustomerWeddingSubmission> builder)
    {
        builder.ToTable("CustomerWeddingSubmissions");
        builder.HasKey(item => item.CustomerWeddingSubmissionId);
        builder.HasIndex(item => item.OrderId).IsUnique();
        builder.Property(item => item.BrideName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.GroomName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(item => item.VenueName).HasMaxLength(200);
        builder.Property(item => item.VenueAddress).HasMaxLength(500);
        builder.Property(item => item.LocationUrl).HasMaxLength(1000);
        builder.Property(item => item.CustomizationsJson).HasMaxLength(20000);
        builder.Property(item => item.AdminReviewedByUserId).HasMaxLength(450);
        builder.HasOne<Order>().WithOne().HasForeignKey<CustomerWeddingSubmission>(item => item.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdminUser>().WithMany().HasForeignKey(item => item.AdminReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
