using WedLink.Business.Invitations;

namespace WedLink.Business.Dashboard;

public sealed class AdminDashboardService(IAdminDashboardRepository repository, TimeProvider timeProvider)
{
    private const string ReportingTimeZone = "Africa/Cairo";

    public async Task<DashboardSummaryData> GetSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        if (from.HasValue != to.HasValue) throw new DashboardValidationException("Provide both start and end dates.");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById(ReportingTimeZone)).DateTime);
        var end = to ?? today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);
        if (start > end) throw new DashboardValidationException("The start date must not be after the end date.");
        if (end.DayNumber - start.DayNumber > 3660) throw new DashboardValidationException("The date range cannot exceed ten years.");
        var startLocal = start.ToDateTime(TimeOnly.MinValue);
        var endExclusiveLocal = end.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var range = new DashboardDateRange(start, end,
            WeddingTimePolicy.ToUtc(startLocal, ReportingTimeZone),
            WeddingTimePolicy.ToUtc(endExclusiveLocal, ReportingTimeZone));
        return await repository.GetSummaryAsync(range, timeProvider.GetUtcNow(), cancellationToken);
    }
}
