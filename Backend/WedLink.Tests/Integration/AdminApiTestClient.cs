using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WedLink.Tests.Integration;

internal static class AdminApiTestClient
{
    private const string Password = "TestPassword8!";

    public static async Task<HttpClient> CreateLoggedInSuperAdminAsync(
        WedLinkApiFactory factory)
    {
        await factory.CreateDatabaseAsync();
        await factory.CreateSuperAdminAsync("admin@example.test", Password);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var csrfToken = await GetCsrfTokenAsync(client);
        using var loginRequest = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/admin/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "admin@example.test",
                password = Password
            })
        };
        loginRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var loginResponse = await client.SendAsync(loginRequest);
        if (!loginResponse.IsSuccessStatusCode)
        {
            client.Dispose();
            throw new InvalidOperationException(
                $"Test admin could not sign in. Status: {(int)loginResponse.StatusCode}.");
        }

        return client;
    }

    public static async Task<HttpResponseMessage> SendSecureJsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body = null)
    {
        var csrfToken = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/admin/auth/csrf");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CsrfResponse>();
        return result?.Token
            ?? throw new InvalidOperationException("CSRF endpoint returned no token.");
    }

    private sealed record CsrfResponse(string Token);
}
