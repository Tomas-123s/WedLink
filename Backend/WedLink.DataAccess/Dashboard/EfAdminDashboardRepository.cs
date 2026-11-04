using Microsoft.EntityFrameworkCore;
using WedLink.Business.Dashboard;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Dashboard;

public sealed class EfAdminDashboardRepository(WedLinkDbContext db) : IAdminDashboardRepository
{
    public async Task<DashboardSummaryData> GetSummaryAsync(DashboardDateRange range, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var orders = db.Orders.Where(o => o.DeletedAt == null && o.CreatedAt >= range.FromUtc && o.CreatedAt < range.ToExclusiveUtc);
        var counts = await orders.GroupBy(o => o.Status).Select(g => new DashboardStatusCountData(g.Key, g.Count())).ToListAsync(cancellationToken);
        var orderIds = orders.Select(o => o.OrderId);
        var payments = await (from p in db.OrderPayments join o in db.Orders on p.OrderId equals o.OrderId
            where o.DeletedAt == null && p.RecordedAt >= range.FromUtc && p.RecordedAt < range.ToExclusiveUtc
            group p by o.Currency into g select new CurrencyAmountData(g.Key, g.Sum(x => x.Amount))).ToListAsync(cancellationToken);
        var orderRows = await orders.Select(o => new { o.OrderId, o.AgreedBasePrice, o.Currency }).ToListAsync(cancellationToken);
        var ids = orderRows.Select(x => x.OrderId).ToArray();
        var addonTotals = await db.OrderAddOns.Where(a => ids.Contains(a.OrderId)).GroupBy(a => a.OrderId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(a => a.PriceAtPurchase) }).ToDictionaryAsync(x => x.Id, x => x.Amount, cancellationToken);
        var paymentTotals = await db.OrderPayments.Where(p => ids.Contains(p.OrderId)).GroupBy(p => p.OrderId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.Id, x => x.Amount, cancellationToken);
        var balances = orderRows.GroupBy(o => o.Currency).Select(g => new CurrencyAmountData(g.Key,
            g.Sum(o => o.AgreedBasePrice + addonTotals.GetValueOrDefault(o.OrderId) - paymentTotals.GetValueOrDefault(o.OrderId)))).ToList();
        var published = await db.Invitations.CountAsync(i => i.PublishedAt >= range.FromUtc && i.PublishedAt < range.ToExclusiveUtc && db.Orders.Any(o => o.OrderId == i.OrderId && o.DeletedAt == null), cancellationToken);
        var expired = await db.Invitations.CountAsync(i => i.ExpiresAt >= range.FromUtc && i.ExpiresAt < range.ToExclusiveUtc && (i.Status == "Expired" || i.ExpiresAt <= now) && db.Orders.Any(o => o.OrderId == i.OrderId && o.DeletedAt == null), cancellationToken);
        var active = await db.Invitations.CountAsync(i => i.Status == "Published" && i.ExpiresAt > now && db.Orders.Any(o => o.OrderId == i.OrderId && o.DeletedAt == null), cancellationToken);
        var expectedAttendance = await (from response in db.RsvpResponses.AsNoTracking()
                                        join invitation in db.Invitations.AsNoTracking() on response.InvitationId equals invitation.InvitationId
                                        where response.Response == "Yes" && invitation.Status == "Published" && invitation.ExpiresAt > now
                                            && db.Orders.Any(o => o.OrderId == invitation.OrderId && o.DeletedAt == null)
                                        select (long)response.AccompanyingPeople + 1L).SumAsync(cancellationToken);
        return new(range.From, range.To, orderRows.Count, counts.FirstOrDefault(c => c.Status == "New")?.Count ?? 0,
            counts, published, expired, active, expectedAttendance, payments, balances);
    }
}
