namespace WedLink.Business.Orders;

public sealed record OrderAddOnData(
    int AddOnId,
    string NameAtPurchase,
    decimal PriceAtPurchase);

public sealed record OrderAddOnInput(int AddOnId);

public sealed record OrderStatusHistoryData(
    string? PreviousStatus,
    string NewStatus,
    string ChangedByAdminUserId,
    DateTimeOffset ChangedAt,
    string? Note);

public sealed record OrderSummaryData(
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
    DateTimeOffset CreatedAt);

public sealed record OrderDetailData(
    int OrderId,
    int CustomerId,
    string CustomerName,
    int SelectedTemplateVersionId,
    string TemplateName,
    string TemplateVersionNumber,
    string Status,
    decimal AgreedBasePrice,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OrderAddOnData> AddOns,
    IReadOnlyList<OrderStatusHistoryData> StatusHistory)
{
    public decimal AddOnsTotal => AddOns.Sum(addOn => addOn.PriceAtPurchase);

    public decimal TotalAmount => AgreedBasePrice + AddOnsTotal;
}

public sealed record OrderPage(
    IReadOnlyList<OrderSummaryData> Items,
    int Page,
    int PageSize,
    int TotalItems);

public sealed record CreateOrderInput(
    int CustomerId,
    int SelectedTemplateVersionId,
    decimal AgreedBasePrice,
    IReadOnlyList<int> AddOnIds);

public sealed record ChangeOrderStatusInput(string Status, string? Note);

public sealed record OrderAddOnSnapshot(
    int AddOnId,
    string NameAtPurchase,
    decimal PriceAtPurchase);

public sealed record OrderCreationOptions(
    bool CustomerIsActive,
    bool TemplateVersionIsAvailable,
    IReadOnlyList<OrderAddOnSnapshot> AvailableAddOns);

public sealed class OrderValidationException(string message) : Exception(message);

public sealed class OrderNotFoundException() : Exception("Order was not found.");

public interface IOrderRepository
{
    Task<OrderPage> SearchAsync(
        string? search,
        string? status,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderDetailData?> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderCreationOptions> GetCreationOptionsAsync(
        int customerId,
        int templateVersionId,
        CancellationToken cancellationToken = default);

    Task<OrderDetailData> CreateAsync(
        CreateOrderInput input,
        string initialStatus,
        string actorId,
        IReadOnlyList<OrderAddOnSnapshot> addOnSnapshots,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<OrderDetailData?> ChangeStatusAsync(
        int orderId,
        string expectedStatus,
        string newStatus,
        string actorId,
        string? note,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default);
}
