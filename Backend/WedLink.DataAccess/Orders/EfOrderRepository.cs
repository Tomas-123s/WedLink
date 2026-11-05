using WedLink.DataAccess.Identity;
using Microsoft.EntityFrameworkCore;
using WedLink.Business.Orders;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Orders;

public sealed class EfOrderRepository(WedLinkDbContext dbContext) : IOrderRepository
{
    public async Task<OrderPage> SearchAsync(
        string? search,
        string? status,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from order in dbContext.Orders.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking()
                on order.CustomerId equals customer.CustomerId
            join version in dbContext.TemplateVersions.AsNoTracking()
                on order.SelectedTemplateVersionId equals version.TemplateVersionId
            join template in dbContext.Templates.AsNoTracking()
                on version.TemplateId equals template.TemplateId
            where order.DeletedAt == null
            select new { order, customer, version, template };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.order.Status == status);
        }

        if (customerId.HasValue)
        {
            query = query.Where(item => item.order.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.customer.Name.Contains(search)
                || item.customer.Phone.Contains(search)
                || item.template.Name.Contains(search));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(item => item.order.CreatedAt)
            .ThenByDescending(item => item.order.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.order.OrderId,
                item.order.CustomerId,
                CustomerName = item.customer.Name,
                item.order.SelectedTemplateVersionId,
                TemplateName = item.template.Name,
                TemplateVersionNumber = item.version.VersionNumber,
                item.order.Status,
                item.order.AgreedBasePrice,
                item.order.Currency,
                item.order.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var orderIds = rows.Select(row => row.OrderId).ToList();
        var addOnTotals = await dbContext.OrderAddOns
            .AsNoTracking()
            .Where(addOn => orderIds.Contains(addOn.OrderId))
            .GroupBy(addOn => addOn.OrderId)
            .Select(group => new { OrderId = group.Key, Total = group.Sum(item => item.PriceAtPurchase) })
            .ToDictionaryAsync(item => item.OrderId, item => item.Total, cancellationToken);

        var items = rows.Select(row =>
        {
            var addOnsTotal = addOnTotals.GetValueOrDefault(row.OrderId);
            return new OrderSummaryData(
                row.OrderId,
                row.CustomerId,
                row.CustomerName,
                row.SelectedTemplateVersionId,
                row.TemplateName,
                row.TemplateVersionNumber,
                row.Status,
                row.AgreedBasePrice,
                row.Currency,
                addOnsTotal,
                row.AgreedBasePrice + addOnsTotal,
                row.CreatedAt);
        }).ToList();

        return new OrderPage(items, page, pageSize, totalItems);
    }

    public async Task<OrderDetailData?> GetByIdAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from order in dbContext.Orders.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking()
                on order.CustomerId equals customer.CustomerId
            join version in dbContext.TemplateVersions.AsNoTracking()
                on order.SelectedTemplateVersionId equals version.TemplateVersionId
            join template in dbContext.Templates.AsNoTracking()
                on version.TemplateId equals template.TemplateId
            where order.OrderId == orderId
                && order.DeletedAt == null
            select new
            {
                order.OrderId,
                order.CustomerId,
                CustomerName = customer.Name,
                order.SelectedTemplateVersionId,
                TemplateName = template.Name,
                TemplateVersionNumber = version.VersionNumber,
                order.Status,
                order.AgreedBasePrice,
                order.Currency,
                order.CreatedAt,
                order.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var addOns = await dbContext.OrderAddOns
            .AsNoTracking()
            .Where(addOn => addOn.OrderId == orderId)
            .OrderBy(addOn => addOn.NameAtPurchase)
            .Select(addOn => new OrderAddOnData(
                addOn.AddOnId,
                addOn.NameAtPurchase,
                addOn.PriceAtPurchase))
            .ToListAsync(cancellationToken);

        var history = await dbContext.OrderStatusHistory
            .AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .OrderBy(item => item.ChangedAt)
            .ThenBy(item => item.OrderStatusHistoryId)
            .Select(item => new OrderStatusHistoryData(
                item.PreviousStatus,
                item.NewStatus,
                item.ChangedByAdminUserId,
                item.ChangedAt,
                item.Note))
            .ToListAsync(cancellationToken);

        return new OrderDetailData(
            row.OrderId,
            row.CustomerId,
            row.CustomerName,
            row.SelectedTemplateVersionId,
            row.TemplateName,
            row.TemplateVersionNumber,
            row.Status,
            row.AgreedBasePrice,
            row.Currency,
            row.CreatedAt,
            row.UpdatedAt,
            addOns,
            history);
    }

    public async Task<OrderCreationOptions> GetCreationOptionsAsync(
        int customerId,
        int templateVersionId,
        CancellationToken cancellationToken = default)
    {
        var customerIsActive = await dbContext.Customers.AnyAsync(
            customer => customer.CustomerId == customerId
                && customer.DeletedAt == null,
            cancellationToken);

        var templateVersionIsAvailable = await (
            from version in dbContext.TemplateVersions.AsNoTracking()
            join template in dbContext.Templates.AsNoTracking()
                on version.TemplateId equals template.TemplateId
            where version.TemplateVersionId == templateVersionId
                && version.IsActive
                && template.IsActive
            select version.TemplateVersionId)
            .AnyAsync(cancellationToken);

        var availableAddOns = await (
            from link in dbContext.TemplateAddOns.AsNoTracking()
            join addOn in dbContext.AddOns.AsNoTracking()
                on link.AddOnId equals addOn.AddOnId
            where link.TemplateVersionId == templateVersionId && addOn.IsActive
            orderby addOn.Name
            select new OrderAddOnSnapshot(
                addOn.AddOnId,
                addOn.Name,
                addOn.CurrentPrice))
            .ToListAsync(cancellationToken);

        return new OrderCreationOptions(
            customerIsActive,
            templateVersionIsAvailable,
            availableAddOns);
    }

    public async Task<OrderDetailData> CreateAsync(
        CreateOrderInput input,
        string initialStatus,
        string actorId,
        IReadOnlyList<OrderAddOnSnapshot> addOnSnapshots,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var order = new Order
        {
            CustomerId = input.CustomerId,
            SelectedTemplateVersionId = input.SelectedTemplateVersionId,
            Status = initialStatus,
            AgreedBasePrice = input.AgreedBasePrice,
            Currency = "EGP",
            CreatedAt = createdAt
        };

        dbContext.Orders.Add(order);
        foreach (var snapshot in addOnSnapshots)
        {
            dbContext.OrderAddOns.Add(new OrderAddOn
            {
                Order = order,
                AddOnId = snapshot.AddOnId,
                NameAtPurchase = snapshot.NameAtPurchase,
                PriceAtPurchase = snapshot.PriceAtPurchase
            });
        }

        dbContext.OrderStatusHistory.Add(new OrderStatusHistory
        {
            Order = order,
            PreviousStatus = null,
            NewStatus = initialStatus,
            ChangedByAdminUserId = actorId,
            ChangedAt = createdAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(order.OrderId, cancellationToken))!;
    }

    public async Task<OrderDetailData?> ChangeStatusAsync(
        int orderId,
        string expectedStatus,
        string newStatus,
        string actorId,
        string? note,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(
            item => item.OrderId == orderId
                && item.DeletedAt == null
                && item.Status == expectedStatus,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Status = newStatus;
        order.UpdatedAt = changedAt;
        dbContext.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            PreviousStatus = expectedStatus,
            NewStatus = newStatus,
            ChangedByAdminUserId = actorId,
            ChangedAt = changedAt,
            Note = note
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(orderId, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        int orderId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(
            item => item.OrderId == orderId && item.DeletedAt == null,
            cancellationToken);
        if (order is null) return false;

        order.DeletedAt = deletedAt;
        order.UpdatedAt = deletedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<OrderBalanceData?> GetBalanceAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Where(item => item.OrderId == orderId && item.DeletedAt == null)
            .Select(item => new { item.OrderId, item.AgreedBasePrice, item.Currency })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null) return null;

        var addOnsTotal = await dbContext.OrderAddOns.AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .SumAsync(item => (decimal?)item.PriceAtPurchase, cancellationToken) ?? 0m;
        var payments = await dbContext.OrderPayments.AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .OrderBy(item => item.RecordedAt)
            .ThenBy(item => item.OrderPaymentId)
            .Select(item => new OrderPaymentData(
                item.OrderPaymentId, item.OrderId, item.Amount, order.Currency,
                item.Method, item.Reference, item.Note, item.RecordedByAdminUserId,
                item.RecordedAt))
            .ToListAsync(cancellationToken);

        var total = order.AgreedBasePrice + addOnsTotal;
        var paid = payments.Sum(item => item.Amount);
        return new OrderBalanceData(order.OrderId, order.AgreedBasePrice, addOnsTotal,
            total, paid, total - paid, order.Currency, payments);
    }

    public async Task<OrderPaymentData?> RecordPaymentAsync(
        int orderId,
        RecordOrderPaymentInput input,
        string actorId,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(
            item => item.OrderId == orderId && item.DeletedAt == null, cancellationToken);
        if (order is null) return null;

        var payment = new OrderPayment
        {
            OrderId = orderId,
            Amount = input.Amount,
            Method = input.Method,
            Reference = input.Reference,
            Note = input.Note,
            RecordedByAdminUserId = actorId,
            RecordedAt = recordedAt
        };
        dbContext.OrderPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new OrderPaymentData(payment.OrderPaymentId, payment.OrderId,
            payment.Amount, order.Currency, payment.Method, payment.Reference,
            payment.Note, payment.RecordedByAdminUserId, payment.RecordedAt);
    }

    public async Task<OrderPaymentData?> CorrectPaymentAsync(
        int paymentId,
        CorrectOrderPaymentInput input,
        string actorId,
        DateTimeOffset correctedAt,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OrderPayments
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.OrderPaymentId == paymentId
                && item.Order != null && item.Order.DeletedAt == null, cancellationToken);
        if (payment?.Order is null) return null;

        dbContext.OrderPaymentCorrections.Add(new OrderPaymentCorrection
        {
            OrderPaymentId = payment.OrderPaymentId,
            PreviousAmount = payment.Amount,
            PreviousMethod = payment.Method,
            PreviousReference = payment.Reference,
            PreviousNote = payment.Note,
            NewAmount = input.Amount,
            NewMethod = input.Method,
            NewReference = input.Reference,
            NewNote = input.Note,
            Reason = input.Reason,
            CorrectedByAdminUserId = actorId,
            CorrectedAt = correctedAt
        });
        payment.Amount = input.Amount;
        payment.Method = input.Method;
        payment.Reference = input.Reference;
        payment.Note = input.Note;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderPaymentData(payment.OrderPaymentId, payment.OrderId,
            payment.Amount, payment.Order.Currency, payment.Method, payment.Reference,
            payment.Note, payment.RecordedByAdminUserId, payment.RecordedAt);
    }
}
