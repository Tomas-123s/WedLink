using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class TemplateFeatureConfiguration : IEntityTypeConfiguration<TemplateFeature>
{
    public void Configure(EntityTypeBuilder<TemplateFeature> builder)
    {
        builder.HasKey(item => new { item.TemplateVersionId, item.FeatureId });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_TemplateFeatures_MaximumCount",
                "[MaximumCount] IS NULL OR [MaximumCount] >= 0");
            table.HasCheckConstraint(
                "CK_TemplateFeatures_MaximumFileSizeMb",
                "[MaximumFileSizeMb] IS NULL OR [MaximumFileSizeMb] > 0");
        });

        builder.HasOne<TemplateVersion>()
            .WithMany()
            .HasForeignKey(item => item.TemplateVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Feature>()
            .WithMany()
            .HasForeignKey(item => item.FeatureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
