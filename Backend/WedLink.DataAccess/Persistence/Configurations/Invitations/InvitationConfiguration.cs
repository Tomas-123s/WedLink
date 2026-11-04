using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Invitations;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Templates;
using WedLink.DataAccess.Guests;

namespace WedLink.DataAccess.Persistence.Configurations.Invitations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.HasKey(item => item.InvitationId);
        builder.Property(item => item.Slug).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(30).IsRequired();
        builder.Property(item => item.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(item => item.CoupleNames).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.VenueName).HasMaxLength(200);
        builder.Property(item => item.VenueAddress).HasMaxLength(500);
        builder.Property(item => item.LocationUrl).HasMaxLength(1000);
        builder.Property(item => item.FinalPngStorageKey).HasMaxLength(200);
        builder.Property(item => item.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(item => item.OrderId).IsUnique();
        builder.HasIndex(item => item.Slug).IsUnique().HasFilter("[Slug] <> N''");
        builder.HasIndex(item => new { item.Status, item.ExpiresAt });
        builder.HasOne<Order>().WithOne().HasForeignKey<Invitation>(item => item.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TemplateVersion>().WithMany().HasForeignKey(item => item.TemplateVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MusicTrack>().WithMany().HasForeignKey(item => item.MusicTrackId).OnDelete(DeleteBehavior.Restrict);
    }
}
