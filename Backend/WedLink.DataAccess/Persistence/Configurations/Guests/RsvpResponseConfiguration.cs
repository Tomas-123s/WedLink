using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Guests;
using WedLink.DataAccess.Invitations;

namespace WedLink.DataAccess.Persistence.Configurations.Guests;

public sealed class RsvpResponseConfiguration : IEntityTypeConfiguration<RsvpResponse>
{
    public void Configure(EntityTypeBuilder<RsvpResponse> builder)
    {
        builder.ToTable("RsvpResponses", table => table.HasCheckConstraint("CK_RsvpResponses_AccompanyingPeople", "[AccompanyingPeople] >= 0"));
        builder.HasKey(item => item.RsvpResponseId);
        builder.Property(item => item.Response).HasMaxLength(10).IsRequired();
        builder.Property(item => item.GuestName).HasMaxLength(150);
        builder.Property(item => item.EditTokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(item => new { item.InvitationId, item.EditTokenHash }).IsUnique();
        builder.HasIndex(item => new { item.InvitationId, item.SubmittedAt });
        builder.HasOne<Invitation>().WithMany().HasForeignKey(item => item.InvitationId).OnDelete(DeleteBehavior.Restrict);
    }
}
