using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Invitations;

namespace WedLink.DataAccess.Persistence.Configurations.Invitations;

public sealed class InvitationCustomizationConfiguration : IEntityTypeConfiguration<InvitationCustomization>
{
    public void Configure(EntityTypeBuilder<InvitationCustomization> builder)
    {
        builder.ToTable("InvitationCustomizations");
        builder.HasKey(item => item.InvitationCustomizationId);
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ValueJson).HasMaxLength(20000).IsRequired();
        builder.HasIndex(item => new { item.InvitationId, item.Key }).IsUnique();
        builder.HasOne<Invitation>().WithMany().HasForeignKey(item => item.InvitationId).OnDelete(DeleteBehavior.Cascade);
    }
}
