using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Guests;

namespace WedLink.DataAccess.Persistence.Configurations.Guests;

public sealed class MusicTrackConfiguration : IEntityTypeConfiguration<MusicTrack>
{
    public void Configure(EntityTypeBuilder<MusicTrack> builder)
    {
        builder.ToTable("MusicTracks");
        builder.HasKey(item => item.MusicTrackId);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Artist).HasMaxLength(150);
        builder.Property(item => item.RightsReference).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
        builder.HasIndex(item => new { item.IsActive, item.Title });
    }
}
