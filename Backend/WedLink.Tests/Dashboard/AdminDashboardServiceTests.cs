using WedLink.Business.Dashboard;

namespace WedLink.Tests.Dashboard;

public sealed class AdminDashboardServiceTests
{
    [Fact]
    public async Task SummaryDefaultsToCurrentCairoCalendarMonth()
    {
        var repository = new CapturingRepository();
        var service = new AdminDashboardService(repository, new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero)));

        await service.GetSummaryAsync(null, null);

        Assert.Equal(new DateOnly(2026, 11, 1), repository.Range!.From);
        Assert.Equal(new DateOnly(2026, 11, 4), repository.Range.To);
        Assert.Equal(new DateTimeOffset(2026, 10, 31, 22, 0, 0, TimeSpan.Zero), repository.Range.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 22, 0, 0, TimeSpan.Zero), repository.Range.ToExclusiveUtc);
    }

    [Fact]
    public async Task SummaryRejectsSingleBoundaryAndReversedOrExcessiveRanges()
    {
        var service = new AdminDashboardService(new CapturingRepository(), new FixedTimeProvider(DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DashboardValidationException>(() => service.GetSummaryAsync(new DateOnly(2026, 1, 1), null));
        await Assert.ThrowsAsync<DashboardValidationException>(() => service.GetSummaryAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)));
        await Assert.ThrowsAsync<DashboardValidationException>(() => service.GetSummaryAsync(new DateOnly(2010, 1, 1), new DateOnly(2026, 1, 1)));
    }

    private sealed class CapturingRepository : IAdminDashboardRepository
    {
        public DashboardDateRange? Range { get; private set; }
        public Task<DashboardSummaryData> GetSummaryAsync(DashboardDateRange range, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            Range = range;
            return Task.FromResult(new DashboardSummaryData(range.From, range.To, 0, 0, [], 0, 0, 0, [], []));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
