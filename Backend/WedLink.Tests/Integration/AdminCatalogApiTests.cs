using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WedLink.Tests.Integration;

public sealed class AdminCatalogApiTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CustomerApi_CreatesSearchesAndSoftDeletesCustomers()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);

        var createResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/customers",
            new
            {
                name = "Sara Ali",
                phone = "010 22 33 44",
                email = "sara@example.test",
                notes = "Bride"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("010223344", created.Phone);

        var duplicateResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/customers",
            new { name = "Another Customer", phone = "010223344" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var searchResponse = await client.GetAsync(
            "/api/v1/admin/customers?search=Sara&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var page = await searchResponse.Content.ReadFromJsonAsync<CustomerPageResponse>(JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalItems);

        var deleteResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Delete,
            $"/api/v1/admin/customers/{created.CustomerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var deletedResponse = await client.GetAsync(
            $"/api/v1/admin/customers/{created.CustomerId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task TemplateApi_StoresFeaturesAddOnsAndOneActiveVersion()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);

        var templateResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/templates",
            new { name = "Classic", description = "Classic invitation", previewImageReference = (string?)null, currentPrice = 1250m });
        Assert.Equal(HttpStatusCode.Created, templateResponse.StatusCode);
        var template = await templateResponse.Content.ReadFromJsonAsync<TemplateResponse>(JsonOptions);
        Assert.NotNull(template);
        Assert.Equal(1250m, template.CurrentPrice);

        var firstVersionResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/templates/{template.TemplateId}/versions",
            new { versionNumber = "1.0", packageKey = "classic-v1" });
        Assert.Equal(HttpStatusCode.Created, firstVersionResponse.StatusCode);
        var firstVersion = await firstVersionResponse.Content.ReadFromJsonAsync<TemplateVersionResponse>(JsonOptions);
        Assert.NotNull(firstVersion);

        var secondVersionResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/templates/{template.TemplateId}/versions",
            new { versionNumber = "2.0", packageKey = "classic-v2" });
        Assert.Equal(HttpStatusCode.Created, secondVersionResponse.StatusCode);
        var secondVersion = await secondVersionResponse.Content.ReadFromJsonAsync<TemplateVersionResponse>(JsonOptions);
        Assert.NotNull(secondVersion);

        var featuresResponse = await client.GetAsync("/api/v1/admin/features");
        var features = await featuresResponse.Content.ReadFromJsonAsync<List<FeatureResponse>>(JsonOptions);
        Assert.NotNull(features);

        var featureResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/template-versions/{firstVersion.TemplateVersionId}/features",
            new[]
            {
                new
                {
                    featureId = features[0].FeatureId,
                    isEnabled = true,
                    maximumCount = 10,
                    maximumFileSizeMb = 5
                }
            });
        Assert.Equal(HttpStatusCode.NoContent, featureResponse.StatusCode);
        var secondFeatureResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/template-versions/{secondVersion.TemplateVersionId}/features",
            new[] { new { featureId = features[0].FeatureId, isEnabled = true, maximumCount = 5, maximumFileSizeMb = 4 } });
        Assert.Equal(HttpStatusCode.NoContent, secondFeatureResponse.StatusCode);

        var addOnResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/addons",
            new { name = "Premium Music", description = "Music track", currentPrice = 250m, isActive = true });
        Assert.Equal(HttpStatusCode.Created, addOnResponse.StatusCode);
        var addOn = await addOnResponse.Content.ReadFromJsonAsync<AddOnResponse>(JsonOptions);
        Assert.NotNull(addOn);

        var associationResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/template-versions/{firstVersion.TemplateVersionId}/addons",
            new { addOnIds = new[] { addOn.AddOnId } });
        Assert.Equal(HttpStatusCode.NoContent, associationResponse.StatusCode);

        var activateFirst = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/template-versions/{firstVersion.TemplateVersionId}/activate");
        Assert.Equal(HttpStatusCode.NoContent, activateFirst.StatusCode);
        var activateSecond = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/template-versions/{secondVersion.TemplateVersionId}/activate");
        Assert.Equal(HttpStatusCode.NoContent, activateSecond.StatusCode);

        var firstDetailsResponse = await client.GetAsync(
            $"/api/v1/admin/template-versions/{firstVersion.TemplateVersionId}");
        var firstDetails = await firstDetailsResponse.Content.ReadFromJsonAsync<TemplateVersionResponse>(JsonOptions);
        Assert.NotNull(firstDetails);
        Assert.False(firstDetails.IsActive);
        Assert.Single(firstDetails.Features);
        Assert.Single(firstDetails.AddOns);

        var secondDetailsResponse = await client.GetAsync(
            $"/api/v1/admin/template-versions/{secondVersion.TemplateVersionId}");
        var secondDetails = await secondDetailsResponse.Content.ReadFromJsonAsync<TemplateVersionResponse>(JsonOptions);
        Assert.NotNull(secondDetails);
        Assert.True(secondDetails.IsActive);

        var anonymous = factory.CreateClient();
        var publicResponse = await anonymous.GetAsync("/api/v1/public/templates");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var publicJson = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains("Classic", publicJson);
        Assert.Contains("1250", publicJson);
        Assert.Contains("2.0", publicJson);
        Assert.Contains(features[0].Key, publicJson);
        Assert.DoesNotContain("packageKey", publicJson);
        Assert.DoesNotContain("classic-v1", publicJson);
        Assert.DoesNotContain("classic-v2", publicJson);

        var deactivate = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/templates/{template.TemplateId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        var hiddenResponse = await anonymous.GetAsync("/api/v1/public/templates");
        Assert.Equal("[]", await hiddenResponse.Content.ReadAsStringAsync());
    }

    private sealed record CustomerResponse(int CustomerId, string Phone);
    private sealed record CustomerPageResponse(int TotalItems);
    private sealed record TemplateResponse(int TemplateId, decimal CurrentPrice);
    private sealed record TemplateVersionResponse(
        int TemplateVersionId,
        bool IsActive,
        List<TemplateFeatureResponse> Features,
        List<AddOnResponse> AddOns);
    private sealed record TemplateFeatureResponse(int FeatureId);
    private sealed record FeatureResponse(int FeatureId, string Key);
    private sealed record AddOnResponse(int AddOnId);
}
