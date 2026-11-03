namespace WedLink.DataAccess.Orders;

public class OrderStatusHistory
{
    public int OrderStatusHistoryId { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    public string ChangedByAdminUserId { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; }

    public string? Note { get; set; }
}
