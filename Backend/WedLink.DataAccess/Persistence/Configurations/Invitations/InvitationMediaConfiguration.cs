using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Invitations;

namespace WedLink.DataAccess.Persistence.Configurations.Invitations;

public sealed class InvitationMediaConfiguration : IEntityTypeConfiguration<InvitationMedia>
{
    public void Configure(EntityTypeBuilder<InvitationMedia> builder)
    {
        builder.ToTable("InvitationMedia", table =>
        {
            table.HasCheckConstraint("CK_InvitationMedia_Size_NonNegative", "[SizeBytes] >= 0");
            table.HasCheckConstraint("CK_InvitationMedia_SortOrder_NonNegative", "[SortOrder] >= 0");
        });
        builder.HasKey(item => item.InvitationMediaId);
        builder.Property(item => item.MediaType).HasMaxLength(30).IsRequired();
        builder.Property(item => item.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.StorageKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(item => new { item.InvitationId, item.DeletedAt, item.SortOrder });
        builder.HasOne<Invitation>().WithMany().HasForeignKey(item => item.InvitationId).OnDelete(DeleteBehavior.Cascade);
    }
}
