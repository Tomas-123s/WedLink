using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.HasKey(feature => feature.FeatureId);

        builder.Property(feature => feature.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(feature => feature.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(feature => feature.Description)
            .HasMaxLength(500);

        builder.HasIndex(feature => feature.Key)
            .IsUnique();

        builder.HasData(
            new Feature { FeatureId = 1, Key = "gallery", Name = "معرض الصور" },
            new Feature { FeatureId = 2, Key = "video", Name = "الفيديو" },
            new Feature { FeatureId = 3, Key = "music", Name = "الموسيقى" },
            new Feature { FeatureId = 4, Key = "countdown", Name = "العد التنازلي" },
            new Feature { FeatureId = 5, Key = "rsvp", Name = "تأكيد الحضور" },
            new Feature { FeatureId = 6, Key = "guest-messages", Name = "رسائل التهنئة" },
            new Feature { FeatureId = 7, Key = "guest-count", Name = "عدد الحضور" });
    }
}
