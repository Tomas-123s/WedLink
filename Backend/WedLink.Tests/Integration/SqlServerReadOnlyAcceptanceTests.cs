using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WedLink.Business.Dashboard;
using WedLink.Business.Invitations;
using WedLink.DataAccess.Dashboard;
using WedLink.DataAccess.Invitations;
using WedLink.DataAccess.Persistence;

namespace WedLink.Tests.Integration;

public sealed class SqlServerReadOnlyAcceptanceTests
{
    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task CriticalDashboardAndExpirationQueriesExecuteOnSqlServer()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<global::Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("WedLinkDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("No WedLinkDb connection string is configured for SQL Server acceptance checks.");

        var options = new DbContextOptionsBuilder<WedLinkDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var db = new WedLinkDbContext(options);
        Assert.True(await db.Database.CanConnectAsync());

        var now = DateTimeOffset.UtcNow;
        var range = new DashboardDateRange(DateOnly.FromDateTime(now.UtcDateTime.Date.AddDays(-1)),
            DateOnly.FromDateTime(now.UtcDateTime.Date), now.AddDays(-1), now.AddDays(1));
        var summary = await new EfAdminDashboardRepository(db).GetSummaryAsync(range, now);
        Assert.True(summary.ExpectedAttendance >= 0);

        var changedInvitations = await new EfInvitationRepository(db, TimeProvider.System)
            .ExpireDueInvitationsAsync(DateTimeOffset.MinValue);
        Assert.Equal(0, changedInvitations);
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("WEDLINK_RUN_SQLSERVER_ACCEPTANCE") != "1")
            Skip = "Set WEDLINK_RUN_SQLSERVER_ACCEPTANCE=1 to run read-only SQL Server acceptance checks.";
    }
}
