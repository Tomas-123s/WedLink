using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WedLink.DataAccess.Persistence;
using WedLink.Tests.Integration;

namespace WedLink.Tests.Orders;

public sealed class OrderApiTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateOrderSnapshotsPricesSearchesAndEnforcesWorkflow()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        var fixture = await CreateCatalogFixtureAsync(client);
        var sessionResponse = await client.GetAsync("/api/v1/admin/auth/me");
        var session = await sessionResponse.Content.ReadFromJsonAsync<AdminSession>(JsonOptions);
        Assert.NotNull(session);

        var createResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/orders",
            new
            {
                customerId = fixture.CustomerId,
                selectedTemplateVersionId = fixture.TemplateVersionId,
                agreedBasePrice = 1500.50m,
                addOnIds = new[] { fixture.AddOnId }
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("New", created.Status);
        Assert.Equal(1500.50m, created.AgreedBasePrice);
        Assert.Equal(250m, created.AddOnsTotal);
        Assert.Equal(1750.50m, created.TotalAmount);
        Assert.Single(created.AddOns);
        Assert.Equal("Premium Music", created.AddOns[0].NameAtPurchase);
        Assert.Equal(250m, created.AddOns[0].PriceAtPurchase);

        var invalidPayment = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/orders/{created.OrderId}/payments",
            new { amount = 0m, method = "Cash", reference = (string?)null, note = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPayment.StatusCode);
        var paymentResponse = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/orders/{created.OrderId}/payments",
            new { amount = 500.25m, method = "Cash", reference = "receipt-1", note = "deposit" });
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<OrderPaymentResponse>(JsonOptions);
        Assert.NotNull(payment);
        Assert.Equal(500.25m, payment.Amount);
        var balanceResponse = await client.GetAsync($"/api/v1/admin/orders/{created.OrderId}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<OrderBalanceResponse>(JsonOptions);
        Assert.NotNull(balance);
        Assert.Equal(1250.25m, balance.Balance);
        Assert.Equal(500.25m, balance.PaidAmount);
        var initialHistory = Assert.Single(created.StatusHistory);
        Assert.Null(initialHistory.PreviousStatus);
        Assert.Equal("New", initialHistory.NewStatus);
        Assert.Equal(session.Id, initialHistory.ChangedByAdminUserId);

        var updateAddOnResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/addons/{fixture.AddOnId}",
            new { name = "Updated Music", description = "Changed", currentPrice = 999m, isActive = true });
        Assert.Equal(HttpStatusCode.OK, updateAddOnResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/api/v1/admin/orders/{created.OrderId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(250m, detail.AddOns[0].PriceAtPurchase);
        Assert.Equal("Premium Music", detail.AddOns[0].NameAtPurchase);

        var unlinkedAddOnResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/addons",
            new { name = "Unlinked", description = (string?)null, currentPrice = 50m, isActive = true });
        Assert.Equal(HttpStatusCode.Created, unlinkedAddOnResponse.StatusCode);
        var unlinkedAddOn = await unlinkedAddOnResponse.Content.ReadFromJsonAsync<AddOnResponse>(JsonOptions);
        Assert.NotNull(unlinkedAddOn);

        var rejectedAddOnOrder = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/orders",
            new
            {
                customerId = fixture.CustomerId,
                selectedTemplateVersionId = fixture.TemplateVersionId,
                agreedBasePrice = 100m,
                addOnIds = new[] { unlinkedAddOn.AddOnId }
            });
        Assert.Equal(HttpStatusCode.BadRequest, rejectedAddOnOrder.StatusCode);

        var invalidStatusFilter = await client.GetAsync("/api/v1/admin/orders?status=Unknown");
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatusFilter.StatusCode);

        var searchResponse = await client.GetAsync(
            $"/api/v1/admin/orders?search=Sara&status=New&customerId={fixture.CustomerId}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var page = await searchResponse.Content.ReadFromJsonAsync<OrderPageResponse>(JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(created.OrderId, Assert.Single(page.Items).OrderId);

        var invalidTransition = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/admin/orders/{created.OrderId}/status",
            new { status = "Completed", note = "skip workflow" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidTransition.StatusCode);

        foreach (var nextStatus in new[] { "Confirmed", "InProgress", "Completed" })
        {
            var response = await AdminApiTestClient.SendSecureJsonAsync(
                client,
                HttpMethod.Patch,
                $"/api/v1/admin/orders/{created.OrderId}/status",
                new { status = nextStatus, note = "workflow test" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var completedResponse = await client.GetAsync($"/api/v1/admin/orders/{created.OrderId}");
        var completed = await completedResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);
        Assert.Equal(4, completed.StatusHistory.Count);

        var terminalTransition = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/admin/orders/{created.OrderId}/status",
            new { status = "Cancelled", note = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, terminalTransition.StatusCode);
    }

    [Fact]
    public async Task DeleteOrderHidesItAndPreservesPaymentAndStatusHistory()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        var fixture = await CreateCatalogFixtureAsync(client);
        var createResponse = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/orders", new
            {
                customerId = fixture.CustomerId,
                selectedTemplateVersionId = fixture.TemplateVersionId,
                agreedBasePrice = 1000m,
                addOnIds = Array.Empty<int>()
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);

        var paymentResponse = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/orders/{order.OrderId}/payments",
            new { amount = 200m, method = "Cash", reference = (string?)null, note = (string?)null });
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        var deleteResponse = await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Delete,
            $"/api/v1/admin/orders/{order.OrderId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/admin/orders?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<OrderPageResponse>(JsonOptions);
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalItems);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/admin/orders/{order.OrderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/admin/orders/{order.OrderId}/balance")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await AdminApiTestClient.SendSecureJsonAsync(client, HttpMethod.Delete,
                $"/api/v1/admin/orders/{order.OrderId}")).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
        var storedOrder = await db.Orders.SingleAsync(item => item.OrderId == order.OrderId);
        Assert.NotNull(storedOrder.DeletedAt);
        Assert.Equal(storedOrder.DeletedAt, storedOrder.UpdatedAt);
        Assert.True(await db.OrderPayments.AnyAsync(item => item.OrderId == order.OrderId));
        Assert.True(await db.OrderStatusHistory.AnyAsync(item => item.OrderId == order.OrderId));
    }

    [Fact]
    public async Task OrderCreationRejectsDuplicateAddOnsAndInvalidPrice()
    {
        using var factory = new WedLinkApiFactory();
        using var client = await AdminApiTestClient.CreateLoggedInSuperAdminAsync(factory);
        var fixture = await CreateCatalogFixtureAsync(client);

        var duplicateAddOnResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/orders",
            new
            {
                customerId = fixture.CustomerId,
                selectedTemplateVersionId = fixture.TemplateVersionId,
                agreedBasePrice = 100m,
                addOnIds = new[] { fixture.AddOnId, fixture.AddOnId }
            });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateAddOnResponse.StatusCode);

        var invalidPriceResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/orders",
            new
            {
                customerId = fixture.CustomerId,
                selectedTemplateVersionId = fixture.TemplateVersionId,
                agreedBasePrice = -1m,
                addOnIds = Array.Empty<int>()
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPriceResponse.StatusCode);

        var cancelFromNewResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/orders",
            new
            {
                customerId = fixture.CustomerId,
                selectedTemplateVersionId = fixture.TemplateVersionId,
                agreedBasePrice = 100m,
                addOnIds = Array.Empty<int>()
            });
        Assert.Equal(HttpStatusCode.Created, cancelFromNewResponse.StatusCode);
        var newOrder = await cancelFromNewResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(newOrder);

        var cancelResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Patch,
            $"/api/v1/admin/orders/{newOrder.OrderId}/status",
            new { status = "Cancelled", note = "cancelled before confirmation" });
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
    }

    private static async Task<OrderFixture> CreateCatalogFixtureAsync(HttpClient client)
    {
        var customerResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/customers",
            new { name = "Sara Ali", phone = "010223344", email = "sara@example.test", notes = (string?)null });
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions);
        Assert.NotNull(customer);

        var templateResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/templates",
            new { name = "Classic", description = (string?)null, previewImageReference = (string?)null });
        Assert.Equal(HttpStatusCode.Created, templateResponse.StatusCode);
        var template = await templateResponse.Content.ReadFromJsonAsync<TemplateResponse>(JsonOptions);
        Assert.NotNull(template);

        var versionResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/templates/{template.TemplateId}/versions",
            new { versionNumber = "1.0", packageKey = "classic-v1" });
        Assert.Equal(HttpStatusCode.Created, versionResponse.StatusCode);
        var version = await versionResponse.Content.ReadFromJsonAsync<TemplateVersionResponse>(JsonOptions);
        Assert.NotNull(version);

        var activateResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/template-versions/{version.TemplateVersionId}/activate");
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        var addOnResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/addons",
            new { name = "Premium Music", description = "Music", currentPrice = 250m, isActive = true });
        Assert.Equal(HttpStatusCode.Created, addOnResponse.StatusCode);
        var addOn = await addOnResponse.Content.ReadFromJsonAsync<AddOnResponse>(JsonOptions);
        Assert.NotNull(addOn);

        var linkAddOnResponse = await AdminApiTestClient.SendSecureJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/template-versions/{version.TemplateVersionId}/addons",
            new { addOnIds = new[] { addOn.AddOnId } });
        Assert.Equal(HttpStatusCode.NoContent, linkAddOnResponse.StatusCode);

        return new OrderFixture(customer.CustomerId, version.TemplateVersionId, addOn.AddOnId);
    }

    private sealed record OrderFixture(int CustomerId, int TemplateVersionId, int AddOnId);
    private sealed record AdminSession(string Id);
    private sealed record CustomerResponse(int CustomerId);
    private sealed record TemplateResponse(int TemplateId);
    private sealed record TemplateVersionResponse(int TemplateVersionId);
    private sealed record AddOnResponse(int AddOnId);
    private sealed record OrderAddOnResponse(string NameAtPurchase, decimal PriceAtPurchase);
    private sealed record OrderStatusHistoryResponse(
        string? PreviousStatus,
        string NewStatus,
        string ChangedByAdminUserId);
    private sealed record OrderResponse(
        int OrderId,
        string Status,
        decimal AgreedBasePrice,
        decimal AddOnsTotal,
        decimal TotalAmount,
        List<OrderAddOnResponse> AddOns,
        List<OrderStatusHistoryResponse> StatusHistory);
    private sealed record OrderSummaryResponse(int OrderId);
    private sealed record OrderPageResponse(int TotalItems, List<OrderSummaryResponse> Items);
    private sealed record OrderPaymentResponse(decimal Amount);
    private sealed record OrderBalanceResponse(decimal PaidAmount, decimal Balance);
}
