namespace WedLink.DataAccess.Audit;

public sealed class AuditLog
{
    public long AuditLogId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
