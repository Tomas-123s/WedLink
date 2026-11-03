using System.Net;
using WedLink.Tests.Integration;

namespace WedLink.Tests.Auth;

public sealed class AdminSessionApiTests
{
    [Fact]
    public async Task LogoutEndsSessionAndProtectsAdminEndpoints()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);

        var sessionResponse = await client.GetAsync("/api/v1/admin/auth/me");
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);

        var logoutResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/auth/logout");
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var expiredSessionResponse = await client.GetAsync("/api/v1/admin/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, expiredSessionResponse.StatusCode);

        var protectedResponse = await client.GetAsync("/api/v1/admin/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }
}
