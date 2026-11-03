using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Orders;

namespace WedLink.DataAccess.Persistence.Configurations.Orders;

public sealed class PrivateLinkTokenConfiguration : IEntityTypeConfiguration<PrivateLinkToken>
{
    public void Configure(EntityTypeBuilder<PrivateLinkToken> builder)
    {
        builder.ToTable("PrivateLinkTokens");
        builder.HasKey(item => item.PrivateLinkTokenId);
        builder.Property(item => item.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Purpose).HasMaxLength(30).IsRequired();
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.OrderId, item.Purpose, item.ExpiresAt });
        builder.HasOne<Order>().WithMany().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
