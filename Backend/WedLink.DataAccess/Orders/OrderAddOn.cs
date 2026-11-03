namespace WedLink.DataAccess.Orders;

public class OrderAddOn
{
    public int OrderAddOnId { get; set; }

    public int OrderId { get; set; }

    public int AddOnId { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public string NameAtPurchase { get; set; } = string.Empty;
}
