using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Audit;
using WedLink.DataAccess.Identity;

namespace WedLink.DataAccess.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs"); b.HasKey(x => x.AuditLogId); b.Property(x => x.AdminUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Action).HasMaxLength(120).IsRequired(); b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(120); b.Property(x => x.Outcome).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.CreatedAt); b.HasIndex(x => new { x.AdminUserId, x.CreatedAt });
        b.HasOne<AdminUser>().WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
