namespace WedLink.DataAccess.Orders;

public class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int SelectedTemplateVersionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal AgreedBasePrice { get; set; }

    public string Currency { get; set; } = "EGP";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
