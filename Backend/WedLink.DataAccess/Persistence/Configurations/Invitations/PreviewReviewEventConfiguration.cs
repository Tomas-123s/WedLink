using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Invitations;

namespace WedLink.DataAccess.Persistence.Configurations.Invitations;

public sealed class PreviewReviewEventConfiguration : IEntityTypeConfiguration<PreviewReviewEvent>
{
    public void Configure(EntityTypeBuilder<PreviewReviewEvent> builder)
    {
        builder.ToTable("PreviewReviewEvents");
        builder.HasKey(item => item.PreviewReviewEventId);
        builder.Property(item => item.Source).HasMaxLength(20).IsRequired();
        builder.Property(item => item.Action).HasMaxLength(30).IsRequired();
        builder.Property(item => item.Comment).HasMaxLength(2000);
        builder.Property(item => item.AdminUserId).HasMaxLength(450);
        builder.HasIndex(item => new { item.InvitationId, item.CreatedAt });
        builder.HasOne<Invitation>().WithMany().HasForeignKey(item => item.InvitationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdminUser>().WithMany().HasForeignKey(item => item.AdminUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
