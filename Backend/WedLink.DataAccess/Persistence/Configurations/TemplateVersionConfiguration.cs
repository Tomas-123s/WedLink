using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.HasKey(version => version.TemplateVersionId);

        builder.Property(version => version.VersionNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(version => version.PackageKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(version => version.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(version => new { version.TemplateId, version.VersionNumber })
            .IsUnique();

        builder.HasIndex(version => version.TemplateId)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne<Template>()
            .WithMany()
            .HasForeignKey(version => version.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
