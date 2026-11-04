namespace WedLink.Business.Dashboard;

public sealed record CurrencyAmountData(string Currency, decimal Amount);
public sealed record DashboardStatusCountData(string Status, int Count);
public sealed record DashboardSummaryData(DateOnly From, DateOnly To, int OrdersCreated, int NewOrders,
    IReadOnlyList<DashboardStatusCountData> OrdersByStatus, int InvitationsPublished, int InvitationsExpired,
    int ActiveInvitations, long ExpectedAttendance, IReadOnlyList<CurrencyAmountData> PaymentsCollected,
    IReadOnlyList<CurrencyAmountData> OutstandingBalances);
public sealed record DashboardDateRange(DateOnly From, DateOnly To, DateTimeOffset FromUtc, DateTimeOffset ToExclusiveUtc);

public interface IAdminDashboardRepository
{
    Task<DashboardSummaryData> GetSummaryAsync(DashboardDateRange range, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class DashboardValidationException(string message) : Exception(message);
