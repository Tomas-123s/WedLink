using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.HasKey(template => template.TemplateId);

        builder.Property(template => template.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasMaxLength(1000);

        builder.Property(template => template.PreviewImageReference)
            .HasMaxLength(500);

        builder.Property(template => template.IsActive)
            .HasDefaultValue(true);

        builder.Property(template => template.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(template => template.Name);
    }
}
