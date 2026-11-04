using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Guests;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Invitations;

namespace WedLink.DataAccess.Persistence.Configurations.Guests;

public sealed class GuestMessageConfiguration : IEntityTypeConfiguration<GuestMessage>
{
    public void Configure(EntityTypeBuilder<GuestMessage> builder)
    {
        builder.ToTable("GuestMessages");
        builder.HasKey(item => item.GuestMessageId);
        builder.Property(item => item.GuestName).HasMaxLength(150);
        builder.Property(item => item.Message).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.DeletedByAdminUserId).HasMaxLength(450);
        builder.HasIndex(item => new { item.InvitationId, item.SubmittedAt });
        builder.HasOne<Invitation>().WithMany().HasForeignKey(item => item.InvitationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdminUser>().WithMany().HasForeignKey(item => item.DeletedByAdminUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
