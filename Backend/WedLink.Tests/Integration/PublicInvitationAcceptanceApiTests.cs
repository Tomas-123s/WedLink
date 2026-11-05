using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Guests;
using WedLink.DataAccess.Invitations;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence;
using WedLink.DataAccess.Templates;

namespace WedLink.Tests.Integration;

public sealed class PublicInvitationAcceptanceApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublicShareMetadataUsesPublicNamesAndExpirationSafeFallback()
    {
        using var factory = new WedLinkApiFactory();
        await factory.CreateDatabaseAsync();
        await SeedInvitationsAsync(factory);
        using var client = factory.CreateClient();

        var activeResponse = await client.GetAsync("/api/v1/public/invitations/active-share-slug/share");
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);
        var activeHtml = await activeResponse.Content.ReadAsStringAsync();
        var decodedActiveHtml = WebUtility.HtmlDecode(activeHtml);
        Assert.Contains("og:title\" content=\"دعوة زفاف ليلى وOmar", decodedActiveHtml);
        Assert.Contains("https://public.example.test/invite/active-share-slug", decodedActiveHtml);
        Assert.Contains("og:image", decodedActiveHtml);
        Assert.DoesNotContain("قصر خاص", decodedActiveHtml);

        var expiredResponse = await client.GetAsync("/api/v1/public/invitations/expired-share-slug/share");
        Assert.Equal(HttpStatusCode.OK, expiredResponse.StatusCode);
        var expiredHtml = WebUtility.HtmlDecode(await expiredResponse.Content.ReadAsStringAsync());
        Assert.Contains("دعوة زفاف | WedLink", expiredHtml);
        Assert.Contains("/share-default.png", expiredHtml);
        Assert.DoesNotContain("ليلى", expiredHtml);
        Assert.DoesNotContain("Omar", expiredHtml);
    }

    [Fact]
    public async Task RsvpUpdatesNormalizeNoAndDashboardUsesBackendActiveInvitationAggregate()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        await SeedInvitationsAsync(factory);

        var yesResponse = await client.PostAsJsonAsync("/api/v1/public/invitations/active-rsvp-slug/rsvps",
            new { response = "Yes", guestName = "Sara", accompanyingPeople = 2, expectedAttendance = 500 });
        Assert.Equal(HttpStatusCode.OK, yesResponse.StatusCode);
        var yes = await yesResponse.Content.ReadFromJsonAsync<RsvpResponseModel>(JsonOptions);
        Assert.NotNull(yes);
        Assert.Equal(2, yes.AccompanyingPeople);
        Assert.Equal(3, yes.ExpectedAttendance);

        var noResponse = await client.PostAsJsonAsync("/api/v1/public/invitations/active-rsvp-slug/rsvps",
            new { response = "No", guestName = "Sara", accompanyingPeople = 4, editToken = yes.EditToken, expectedAttendance = 500 });
        Assert.Equal(HttpStatusCode.OK, noResponse.StatusCode);
        var no = await noResponse.Content.ReadFromJsonAsync<RsvpResponseModel>(JsonOptions);
        Assert.NotNull(no);
        Assert.True(no.IsUpdate);
        Assert.Equal(0, no.AccompanyingPeople);
        Assert.Equal(0, no.ExpectedAttendance);

        var interactionsResponse = await client.GetAsync("/api/v1/public/invitations/active-rsvp-slug/guest-interactions");
        var interactions = await interactionsResponse.Content.ReadFromJsonAsync<InteractionsModel>(JsonOptions);
        Assert.NotNull(interactions);
        Assert.Equal(0, interactions.ExpectedAttendance);

        var updatedYesResponse = await client.PostAsJsonAsync("/api/v1/public/invitations/active-rsvp-slug/rsvps",
            new { response = "Yes", guestName = "Sara", accompanyingPeople = 1, editToken = no.EditToken, expectedAttendance = 500 });
        Assert.Equal(HttpStatusCode.OK, updatedYesResponse.StatusCode);
        var updatedYes = await updatedYesResponse.Content.ReadFromJsonAsync<RsvpResponseModel>(JsonOptions);
        Assert.NotNull(updatedYes);
        Assert.Equal(2, updatedYes.ExpectedAttendance);

        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, cairo).DateTime);
        var dashboardResponse = await client.GetAsync($"/api/v1/admin/dashboard/summary?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardModel>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard.ExpectedAttendance);
    }

    private static async Task SeedInvitationsAsync(WedLinkApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
        var now = DateTimeOffset.UtcNow;
        db.Customers.Add(new Customer { CustomerId = 1, Name = "Test Customer", Phone = "01012345678", CreatedAt = now });
        db.Templates.Add(new Template { TemplateId = 1, Name = "Test Template", CreatedAt = now });
        db.TemplateVersions.Add(new TemplateVersion
        {
            TemplateVersionId = 1, TemplateId = 1, VersionNumber = "1.0", PackageKey = "test-v1", IsActive = true, CreatedAt = now
        });
        db.TemplateFeatures.AddRange(
            new TemplateFeature { TemplateVersionId = 1, FeatureId = 5, IsEnabled = true, MaximumCount = 5 },
            new TemplateFeature { TemplateVersionId = 1, FeatureId = 7, IsEnabled = true });
        db.Orders.AddRange(
            new Order { OrderId = 1, CustomerId = 1, SelectedTemplateVersionId = 1, Status = "Completed", AgreedBasePrice = 100m, CreatedAt = now },
            new Order { OrderId = 2, CustomerId = 1, SelectedTemplateVersionId = 1, Status = "Completed", AgreedBasePrice = 100m, CreatedAt = now },
            new Order { OrderId = 3, CustomerId = 1, SelectedTemplateVersionId = 1, Status = "Completed", AgreedBasePrice = 100m, CreatedAt = now.AddDays(-60) });
        db.Invitations.AddRange(
            new Invitation
            {
                InvitationId = 1, OrderId = 1, TemplateVersionId = 1, Slug = "active-share-slug", Status = "Published",
                CoupleNames = "[\"ليلى\",\"Omar\"]", VenueName = "قصر خاص", WeddingDateTimeLocal = DateTime.UtcNow.AddDays(10),
                TimeZoneId = "Africa/Cairo", CreatedAt = now, PublishedAt = now, ExpiresAt = now.AddDays(40)
            },
            new Invitation
            {
                InvitationId = 2, OrderId = 2, TemplateVersionId = 1, Slug = "active-rsvp-slug", Status = "Published",
                CoupleNames = "[\"Sara\",\"Ali\"]", WeddingDateTimeLocal = DateTime.UtcNow.AddDays(10),
                TimeZoneId = "Africa/Cairo", CreatedAt = now, PublishedAt = now, ExpiresAt = now.AddDays(40)
            },
            new Invitation
            {
                InvitationId = 3, OrderId = 3, TemplateVersionId = 1, Slug = "expired-share-slug", Status = "Expired",
                CoupleNames = "[\"ليلى\",\"Omar\"]", WeddingDateTimeLocal = DateTime.UtcNow.AddDays(-50),
                TimeZoneId = "Africa/Cairo", CreatedAt = now.AddDays(-60), PublishedAt = now.AddDays(-60), ExpiresAt = now.AddDays(-10)
            });
        db.RsvpResponses.Add(new RsvpResponse
        {
            InvitationId = 3, Response = "Yes", GuestName = "Old response", AccompanyingPeople = 10,
            EditTokenHash = new string('a', 64), SubmittedAt = now.AddDays(-30)
        });
        await db.SaveChangesAsync();
    }

    private sealed record RsvpResponseModel(int AccompanyingPeople, long ExpectedAttendance, string EditToken, bool IsUpdate);
    private sealed record InteractionsModel(long ExpectedAttendance);
    private sealed record DashboardModel(long ExpectedAttendance);
}
