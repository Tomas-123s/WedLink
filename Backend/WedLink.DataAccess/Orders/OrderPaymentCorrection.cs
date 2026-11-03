namespace WedLink.DataAccess.Orders;

public class OrderPaymentCorrection
{
    public int OrderPaymentCorrectionId { get; set; }

    public int OrderPaymentId { get; set; }

    public decimal PreviousAmount { get; set; }

    public string PreviousMethod { get; set; } = string.Empty;

    public string? PreviousReference { get; set; }

    public string? PreviousNote { get; set; }

    public decimal NewAmount { get; set; }

    public string NewMethod { get; set; } = string.Empty;

    public string? NewReference { get; set; }

    public string? NewNote { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string CorrectedByAdminUserId { get; set; } = string.Empty;

    public DateTimeOffset CorrectedAt { get; set; }
}
