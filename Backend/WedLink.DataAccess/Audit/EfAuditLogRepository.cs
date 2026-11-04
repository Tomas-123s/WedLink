using Microsoft.EntityFrameworkCore;
using WedLink.Business.Audit;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Audit;

public sealed class EfAuditLogRepository(WedLinkDbContext db) : IAuditLogRepository
{
    public async Task<AuditLogPage> SearchAsync(AuditLogFilter f, CancellationToken ct = default)
    {
        var q = from a in db.AuditLogs join u in db.Users on a.AdminUserId equals u.Id select new { a, u.DisplayName };
        if (!string.IsNullOrWhiteSpace(f.UserId)) q = q.Where(x => x.a.AdminUserId == f.UserId);
        if (!string.IsNullOrWhiteSpace(f.Action)) q = q.Where(x => x.a.Action.Contains(f.Action));
        if (!string.IsNullOrWhiteSpace(f.EntityType)) q = q.Where(x => x.a.EntityType.Contains(f.EntityType));
        if (!string.IsNullOrWhiteSpace(f.EntityId)) q = q.Where(x => x.a.EntityId == f.EntityId);
        if (!string.IsNullOrWhiteSpace(f.Outcome)) q = q.Where(x => x.a.Outcome == f.Outcome);
        if (f.FromUtc.HasValue) q = q.Where(x => x.a.CreatedAt >= f.FromUtc);
        if (f.ToExclusiveUtc.HasValue) q = q.Where(x => x.a.CreatedAt < f.ToExclusiveUtc);
        var total = await q.CountAsync(ct); var rows = await q.OrderByDescending(x => x.a.CreatedAt).ThenByDescending(x => x.a.AuditLogId)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).Select(x => new AuditLogRow(x.a.AuditLogId, x.a.AdminUserId, x.DisplayName, x.a.Action, x.a.EntityType, x.a.EntityId, x.a.Outcome, x.a.HttpStatusCode, x.a.CreatedAt)).ToListAsync(ct);
        return new(rows, f.Page, f.PageSize, total, (int)Math.Ceiling(total / (double)f.PageSize));
    }
}
