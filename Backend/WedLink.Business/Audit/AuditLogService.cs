using WedLink.Business.Dashboard;
using WedLink.Business.Invitations;

namespace WedLink.Business.Audit;

public sealed class AuditLogService(IAuditLogRepository repository)
{
    public Task<AuditLogPage> SearchAsync(string? userId, string? action, string? entityType, string? entityId, string? outcome, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new AuditValidationException("Page must be positive and pageSize must be between 1 and 100.");
        if (from.HasValue != to.HasValue || (from.HasValue && from > to)) throw new AuditValidationException("Provide a valid from and to date together.");
        DateTimeOffset? fromUtc = null, toUtc = null;
        if (from.HasValue) { fromUtc = WeddingTimePolicy.ToUtc(from.Value.ToDateTime(TimeOnly.MinValue), "Africa/Cairo"); toUtc = WeddingTimePolicy.ToUtc(to!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), "Africa/Cairo"); }
        return repository.SearchAsync(new(userId, action, entityType, entityId, outcome, fromUtc, toUtc, page, pageSize), ct);
    }
}
