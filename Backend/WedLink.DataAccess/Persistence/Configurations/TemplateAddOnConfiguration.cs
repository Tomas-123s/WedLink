using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class TemplateAddOnConfiguration : IEntityTypeConfiguration<TemplateAddOn>
{
    public void Configure(EntityTypeBuilder<TemplateAddOn> builder)
    {
        builder.HasKey(item => new { item.TemplateVersionId, item.AddOnId });

        builder.HasOne<TemplateVersion>()
            .WithMany()
            .HasForeignKey(item => item.TemplateVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AddOn>()
            .WithMany()
            .HasForeignKey(item => item.AddOnId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
