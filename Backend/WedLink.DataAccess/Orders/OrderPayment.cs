namespace WedLink.DataAccess.Orders;

public class OrderPayment
{
    public int OrderPaymentId { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public string? Note { get; set; }

    public string RecordedByAdminUserId { get; set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; set; }
}
