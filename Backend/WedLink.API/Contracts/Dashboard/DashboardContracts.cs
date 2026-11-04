using WedLink.Business.Dashboard;

namespace WedLink.API.Contracts.Dashboard;

public sealed record DashboardSummaryResponse(DateOnly From, DateOnly To, int OrdersCreated, int NewOrders,
    IReadOnlyList<DashboardStatusCountData> OrdersByStatus, int InvitationsPublished, int InvitationsExpired,
    int ActiveInvitations, IReadOnlyList<CurrencyAmountData> PaymentsCollected, IReadOnlyList<CurrencyAmountData> OutstandingBalances)
{
    public static DashboardSummaryResponse FromData(DashboardSummaryData d) => new(d.From, d.To, d.OrdersCreated, d.NewOrders,
        d.OrdersByStatus, d.InvitationsPublished, d.InvitationsExpired, d.ActiveInvitations, d.PaymentsCollected, d.OutstandingBalances);
}
