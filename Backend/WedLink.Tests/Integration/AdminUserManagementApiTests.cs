using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WedLink.Tests.Integration;

public sealed class AdminUserManagementApiTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SuperAdminCanManageAccountsButCannotDemoteOrDeactivateThemself()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);

        var createResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/users",
            new
            {
                displayName = "Admin Manager",
                email = "manager@example.test",
                password = "ManagerPass8!",
                roles = new[] { "Admin" }
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.IsActive);
        Assert.Equal("manager@example.test", created.Email);
        Assert.Contains("Admin", created.Roles);

        var listResponse = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var accounts = await listResponse.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonOptions);
        Assert.NotNull(accounts);
        Assert.Contains(accounts, account => account.Id == created.Id);

        var promoteResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/users/{created.Id}/roles",
            new { roles = new[] { "Admin", "SuperAdmin" } });
        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);

        var selfRolesResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/users/{GetSuperAdminId(accounts)}/roles",
            new { roles = new[] { "Admin" } });
        Assert.Equal(HttpStatusCode.BadRequest, selfRolesResponse.StatusCode);

        var selfStatusResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/admin/users/{GetSuperAdminId(accounts)}/status",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.BadRequest, selfStatusResponse.StatusCode);
    }

    [Fact]
    public async Task AdminRoleCannotSignIntoAdminApplication()
    {
        using var factory = new WedLinkApiFactory();
        using var superAdminClient = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        var createResponse = await AdminApiTestClient.SendSecureJsonAsync(
            superAdminClient,
            HttpMethod.Post,
            "/api/v1/admin/users",
            new
            {
                displayName = "Admin Only",
                email = "admin-only@example.test",
                password = "AdminPass8!",
                roles = new[] { "Admin" }
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var adminClient = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var loginResponse = await AdminApiTestClient.SendSecureJsonAsync(
            adminClient,
            HttpMethod.Post,
            "/api/v1/admin/auth/login",
            new { email = "admin-only@example.test", password = "AdminPass8!" });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        var protectedResponse = await adminClient.GetAsync("/api/v1/admin/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }


    [Fact]
    public async Task DeactivatingAnotherSuperAdminInvalidatesTheirExistingSession()
    {
        using var factory = new WedLinkApiFactory();
        using var ownerClient = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        var createResponse = await AdminApiTestClient.SendSecureJsonAsync(
            ownerClient,
            HttpMethod.Post,
            "/api/v1/admin/users",
            new
            {
                email = "second-super@example.test",
                password = "SecondPass8!",
                roles = new[] { "SuperAdmin" }
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(JsonOptions);
        Assert.NotNull(created);

        using var secondClient = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var loginResponse = await AdminApiTestClient.SendSecureJsonAsync(
            secondClient,
            HttpMethod.Post,
            "/api/v1/admin/auth/login",
            new { email = "second-super@example.test", password = "SecondPass8!" });
        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await secondClient.GetAsync("/api/v1/admin/auth/me")).StatusCode);

        var deactivateResponse = await AdminApiTestClient.SendSecureJsonAsync(
            ownerClient,
            HttpMethod.Patch,
            $"/api/v1/admin/users/{created.Id}/status",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var staleSessionResponse = await secondClient.GetAsync("/api/v1/admin/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, staleSessionResponse.StatusCode);
    }

    [Fact]
    public async Task AnonymousCannotManageAdminUsers()
    {
        using var factory = new WedLinkApiFactory();
        await factory.CreateDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string GetSuperAdminId(IReadOnlyList<AccountResponse> accounts) =>
        accounts.Single(account => account.Roles.Contains("SuperAdmin")).Id;

    private sealed record AccountResponse(
        string Id,
        string Email,
        bool IsActive,
        List<string> Roles);
}
