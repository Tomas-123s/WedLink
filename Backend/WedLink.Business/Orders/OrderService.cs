namespace WedLink.Business.Orders;

public sealed class OrderService(IOrderRepository repository)
{
    private const decimal MaximumMoneyAmount = 99_999_999.99m;

    public Task<OrderPage> SearchAsync(
        string? search,
        string? status,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || customerId is < 1
            || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new OrderValidationException("Invalid order search or pagination values.");
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!OrderStatuses.TryNormalize(status, out normalizedStatus))
            {
                throw new OrderValidationException("The order status filter is not supported.");
            }
        }

        return repository.SearchAsync(
            CleanOptional(search), normalizedStatus, customerId, page, pageSize,
            cancellationToken);
    }

    public async Task<OrderDetailData> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId < 1)
        {
            throw new OrderNotFoundException();
        }

        return await repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new OrderNotFoundException();
    }

    public async Task<OrderDetailData> CreateAsync(
        CreateOrderInput input,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateInput(input);

        var options = await repository.GetCreationOptionsAsync(
            input.CustomerId, input.SelectedTemplateVersionId, cancellationToken);
        if (!options.CustomerIsActive)
        {
            throw new OrderValidationException("The customer is unavailable.");
        }

        if (!options.TemplateVersionIsAvailable)
        {
            throw new OrderValidationException("The selected template version is unavailable for new orders.");
        }

        var availableById = options.AvailableAddOns.ToDictionary(addOn => addOn.AddOnId);
        if (input.AddOnIds.Any(addOnId => !availableById.ContainsKey(addOnId)))
        {
            throw new OrderValidationException("Select active add-ons available for the chosen template version.");
        }

        var snapshots = input.AddOnIds
            .Select(addOnId => availableById[addOnId])
            .ToList();

        return await repository.CreateAsync(
            input,
            OrderStatuses.New,
            actorId,
            snapshots,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task<OrderDetailData> ChangeStatusAsync(
        int orderId,
        ChangeOrderStatusInput input,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        if (orderId < 1 || !OrderStatuses.TryNormalize(input.Status, out var newStatus))
        {
            throw new OrderValidationException("The requested order status is not supported.");
        }

        var note = CleanOptional(input.Note);
        if (note?.Length > 1000)
        {
            throw new OrderValidationException("The status note cannot exceed 1000 characters.");
        }

        var current = await repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        if (!OrderStatuses.CanTransition(current.Status, newStatus))
        {
            throw new OrderValidationException(
                $"An order cannot move from {current.Status} to {newStatus}.");
        }

        return await repository.ChangeStatusAsync(
                   orderId, current.Status, newStatus, actorId, note,
                   DateTimeOffset.UtcNow, cancellationToken)
               ?? throw new OrderStatusConflictException();
    }

    private static void ValidateCreateInput(CreateOrderInput input)
    {
        if (input.CustomerId < 1 || input.SelectedTemplateVersionId < 1)
        {
            throw new OrderValidationException("A valid customer and template version are required.");
        }

        if (input.AgreedBasePrice < 0
            || input.AgreedBasePrice > MaximumMoneyAmount
            || decimal.Round(input.AgreedBasePrice, 2) != input.AgreedBasePrice)
        {
            throw new OrderValidationException(
                "The agreed base price must be non-negative and have no more than two decimal places.");
        }

        if (input.AddOnIds.Any(addOnId => addOnId < 1)
            || input.AddOnIds.Distinct().Count() != input.AddOnIds.Count)
        {
            throw new OrderValidationException("The add-on list contains an invalid or repeated ID.");
        }
    }

    private static string? CleanOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}

public sealed class OrderStatusConflictException() : Exception(
    "The order changed while the status update was being saved. Reload and try again.");
