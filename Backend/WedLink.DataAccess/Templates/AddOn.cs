namespace WedLink.DataAccess.Templates;

public class AddOn
{
    public int AddOnId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal CurrentPrice { get; set; }

    public string Currency { get; set; } = "EGP";

    public bool IsActive { get; set; } = true;
}
