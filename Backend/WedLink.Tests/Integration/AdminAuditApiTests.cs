using System.Net;
using System.Net.Http.Json;
using WedLink.DataAccess.Audit;
using WedLink.DataAccess.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace WedLink.Tests.Integration;

public sealed class AdminAuditApiTests
{
    [Fact]
    public async Task SuperAdminCanSearchWriteAuditMetadataWithoutPersistingSecrets()
    {
        using var factory = new WedLinkApiFactory();
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/admin/audit-logs")).StatusCode);
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        var create = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Post, "/api/v1/admin/users", new
        {
            displayName = "Audited Admin", email = "audited@example.test", password = "SecretPass8!", roles = new[] { "Admin" }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var response = await client.GetAsync("/api/v1/admin/audit-logs?action=AdminUsers.Create&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("AdminUsers.Create", json);
        Assert.Contains("Succeeded", json);
        Assert.DoesNotContain("SecretPass8!", json);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
        var log = Assert.Single(db.AuditLogs.Where(x => x.Action == "AdminUsers.Create"));
        Assert.Equal("Succeeded", log.Outcome);
        Assert.Equal(201, log.HttpStatusCode);
        Assert.False(string.IsNullOrWhiteSpace(log.EntityId));
    }
}
