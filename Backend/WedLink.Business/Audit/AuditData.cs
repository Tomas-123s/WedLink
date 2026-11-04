namespace WedLink.Business.Audit;

public sealed record AuditLogRow(long Id, string AdminUserId, string AdminDisplayName, string Action, string EntityType, string? EntityId, string Outcome, int HttpStatusCode, DateTimeOffset CreatedAt);
public sealed record AuditLogPage(IReadOnlyList<AuditLogRow> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record AuditLogFilter(string? UserId, string? Action, string? EntityType, string? EntityId, string? Outcome, DateTimeOffset? FromUtc, DateTimeOffset? ToExclusiveUtc, int Page, int PageSize);
public interface IAuditLogRepository { Task<AuditLogPage> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default); }
public sealed class AuditValidationException(string message) : Exception(message);
