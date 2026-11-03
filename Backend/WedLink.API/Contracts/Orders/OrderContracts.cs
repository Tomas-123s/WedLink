using System.ComponentModel.DataAnnotations;
using WedLink.Business.Orders;

namespace WedLink.API.Contracts.Orders;

public sealed record CreateOrderRequest(
    [Range(1, int.MaxValue)] int CustomerId,
    [Range(1, int.MaxValue)] int SelectedTemplateVersionId,
    [Range(typeof(decimal), "0", "99999999.99")] decimal AgreedBasePrice,
    IReadOnlyList<int>? AddOnIds = null)
{
    public CreateOrderInput ToInput() => new(
        CustomerId, SelectedTemplateVersionId, AgreedBasePrice, AddOnIds ?? []);
}

public sealed record ChangeOrderStatusRequest(
    [Required, StringLength(30)] string Status,
    [StringLength(1000)] string? Note)
{
    public ChangeOrderStatusInput ToInput() => new(Status, Note);
}

public sealed record RecordOrderPaymentRequest(
    [Range(typeof(decimal), "0.01", "99999999.99")] decimal Amount,
    [Required, StringLength(40, MinimumLength = 1)] string Method,
    [StringLength(200)] string? Reference,
    [StringLength(1000)] string? Note)
{
    public RecordOrderPaymentInput ToInput() =>
        new(Amount, Method, Reference, Note);
}

public sealed record CorrectOrderPaymentRequest(
    [Range(typeof(decimal), "0.01", "99999999.99")] decimal Amount,
    [Required, StringLength(40, MinimumLength = 1)] string Method,
    [StringLength(200)] string? Reference,
    [StringLength(1000)] string? Note,
    [Required, StringLength(1000, MinimumLength = 1)] string Reason)
{
    public CorrectOrderPaymentInput ToInput() =>
        new(Amount, Method, Reference, Note, Reason);
}

public sealed record OrderPaymentResponse(
    int PaymentId,
    int OrderId,
    decimal Amount,
    string Currency,
    string Method,
    string? Reference,
    string? Note,
    string RecordedByAdminUserId,
    DateTimeOffset RecordedAt)
{
    public static OrderPaymentResponse FromData(OrderPaymentData payment) => new(
        payment.PaymentId, payment.OrderId, payment.Amount, payment.Currency,
        payment.Method, payment.Reference, payment.Note,
        payment.RecordedByAdminUserId, payment.RecordedAt);
}

public sealed record OrderBalanceResponse(
    int OrderId,
    decimal AgreedBasePrice,
    decimal AddOnsTotal,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal Balance,
    string Currency,
    IReadOnlyList<OrderPaymentResponse> Payments)
{
    public static OrderBalanceResponse FromData(OrderBalanceData balance) => new(
        balance.OrderId, balance.AgreedBasePrice, balance.AddOnsTotal,
        balance.TotalAmount, balance.PaidAmount, balance.Balance, balance.Currency,
        balance.Payments.Select(OrderPaymentResponse.FromData).ToList());
}

public sealed record OrderAddOnResponse(
    int AddOnId,
    string NameAtPurchase,
    decimal PriceAtPurchase)
{
    public static OrderAddOnResponse FromData(OrderAddOnData item) =>
        new(item.AddOnId, item.NameAtPurchase, item.PriceAtPurchase);
}

public sealed record OrderStatusHistoryResponse(
    string? PreviousStatus,
    string NewStatus,
    string ChangedByAdminUserId,
    DateTimeOffset ChangedAt,
    string? Note)
{
    public static OrderStatusHistoryResponse FromData(OrderStatusHistoryData item) =>
        new(item.PreviousStatus, item.NewStatus, item.ChangedByAdminUserId, item.ChangedAt, item.Note);
}

public sealed record OrderSummaryResponse(
    int OrderId,
    int CustomerId,
    string CustomerName,
    int SelectedTemplateVersionId,
    string TemplateName,
    string TemplateVersionNumber,
    string Status,
    decimal AgreedBasePrice,
    string Currency,
    decimal AddOnsTotal,
    decimal TotalAmount,
    DateTimeOffset CreatedAt)
{
    public static OrderSummaryResponse FromData(OrderSummaryData item) => new(
        item.OrderId,
        item.CustomerId,
        item.CustomerName,
        item.SelectedTemplateVersionId,
        item.TemplateName,
        item.TemplateVersionNumber,
        item.Status,
        item.AgreedBasePrice,
        item.Currency,
        item.AddOnsTotal,
        item.TotalAmount,
        item.CreatedAt);
}

public sealed record OrderDetailResponse(
    int OrderId,
    int CustomerId,
    string CustomerName,
    int SelectedTemplateVersionId,
    string TemplateName,
    string TemplateVersionNumber,
    string Status,
    decimal AgreedBasePrice,
    string Currency,
    decimal AddOnsTotal,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OrderAddOnResponse> AddOns,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory)
{
    public static OrderDetailResponse FromData(OrderDetailData item) => new(
        item.OrderId,
        item.CustomerId,
        item.CustomerName,
        item.SelectedTemplateVersionId,
        item.TemplateName,
        item.TemplateVersionNumber,
        item.Status,
        item.AgreedBasePrice,
        item.Currency,
        item.AddOnsTotal,
        item.TotalAmount,
        item.CreatedAt,
        item.UpdatedAt,
        item.AddOns.Select(OrderAddOnResponse.FromData).ToList(),
        item.StatusHistory.Select(OrderStatusHistoryResponse.FromData).ToList());
}

public sealed record OrderPageResponse(
    IReadOnlyList<OrderSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static OrderPageResponse FromData(OrderPage page) => new(
        page.Items.Select(OrderSummaryResponse.FromData).ToList(),
        page.Page,
        page.PageSize,
        page.TotalItems,
        page.PageSize == 0 ? 0 : (int)Math.Ceiling((double)page.TotalItems / page.PageSize));
}
