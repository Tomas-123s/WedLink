using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Storage;

namespace WedLink.DataAccess.Persistence.Configurations;

public sealed class StoredMediaFileConfiguration : IEntityTypeConfiguration<StoredMediaFile>
{
    public void Configure(EntityTypeBuilder<StoredMediaFile> builder)
    {
        builder.ToTable("StoredMediaFiles");
        builder.HasKey(item => item.StorageKey);
        builder.Property(item => item.StorageKey).HasMaxLength(200);
        builder.Property(item => item.Content).HasColumnType("varbinary(max)").IsRequired();
    }
}
