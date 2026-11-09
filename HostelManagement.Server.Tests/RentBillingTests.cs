using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HostelManagement.Server.Tests;

public class RentBillingTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var setupResponse = await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Pune Hostels", email, "P@ssw0rd!"));
        var auth = await setupResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.Token);
        return client;
    }

    private static async Task<(Guid ResidentId, Guid CotId)> CreateAssignedResidentAsync(
        HttpClient client, DateOnly moveInDate, decimal rentAmount, BillingCycle billingCycle)
    {
        var hostelResponse = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel One", "Pune"));
        var hostel = await hostelResponse.Content.ReadFromJsonAsync<HostelResponse>();
        var roomResponse = await client.PostAsJsonAsync($"/api/hostels/{hostel!.Id}/rooms", new CreateRoomRequest("Room One"));
        var room = await roomResponse.Content.ReadFromJsonAsync<RoomResponse>();
        var cotResponse = await client.PostAsJsonAsync($"/api/rooms/{room!.Id}/cots", new CreateCotRequest("Cot One"));
        var cot = await cotResponse.Content.ReadFromJsonAsync<CotResponse>(TestJson.Options);

        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", moveInDate));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cot!.Id, rentAmount, rentAmount * 2, billingCycle, moveInDate), TestJson.Options);

        return (resident.Id, cot.Id);
    }

    /// <summary>
    /// Reads payment rows straight from the database to assert on fields the API
    /// does not return (Type, ResidentId). Query filters are ignored because this
    /// runs outside a request, so there is no authenticated owner to scope to.
    /// </summary>
    private async Task<List<Payment>> StoredPaymentsAsync(Guid residentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Payments
            .IgnoreQueryFilters()
            .Where(p => p.ResidentId == residentId)
            .ToListAsync();
    }

    [Fact]
    public async Task No_due_periods_are_generated_for_a_checked_out_resident()
    {
        var client = await AuthenticatedClientAsync("billing1@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        await client.PostAsJsonAsync($"/api/residents/{residentId}/notice",
            new RecordNoticeRequest(new DateOnly(2026, 1, 20), 30));
        await client.PostAsJsonAsync($"/api/residents/{residentId}/checkout", new CheckoutRequest(10000m));

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, duesResponse.StatusCode);
        Assert.Empty(dues!);
    }

    [Fact]
    public async Task Partial_payment_leaves_the_due_period_unpaid()
    {
        var client = await AuthenticatedClientAsync("billing2@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        var firstDue = dues!.First();

        var paymentResponse = await client.PostAsJsonAsync($"/api/dues/{firstDue.Id}/payments",
            new LogPaymentRequest(2000m, new DateOnly(2026, 1, 5)));
        var updated = await paymentResponse.Content.ReadFromJsonAsync<DuePeriodResponse>(TestJson.Options);

        Assert.False(updated!.IsPaid);
        Assert.Equal(2000m, updated.AmountPaid);
    }

    [Fact]
    public async Task Payment_covering_full_rent_marks_the_due_period_paid()
    {
        var client = await AuthenticatedClientAsync("billing3@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        var firstDue = dues!.First();

        var paymentResponse = await client.PostAsJsonAsync($"/api/dues/{firstDue.Id}/payments",
            new LogPaymentRequest(5000m, new DateOnly(2026, 1, 5)));
        var updated = await paymentResponse.Content.ReadFromJsonAsync<DuePeriodResponse>(TestJson.Options);

        Assert.True(updated!.IsPaid);
    }

    [Fact]
    public async Task Payment_logged_against_a_due_period_is_stored_as_a_rent_payment_for_the_resident()
    {
        var client = await AuthenticatedClientAsync("billing5@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        var firstDue = dues!.First();

        await client.PostAsJsonAsync($"/api/dues/{firstDue.Id}/payments",
            new LogPaymentRequest(5000m, new DateOnly(2026, 1, 5)));

        var payment = Assert.Single(await StoredPaymentsAsync(residentId));
        Assert.Equal(PaymentType.Rent, payment.Type);
        Assert.Equal(residentId, payment.ResidentId);
        Assert.Equal(firstDue.Id, payment.DuePeriodId);
    }

    [Fact]
    public async Task Deposit_payment_is_stored_against_the_resident_with_no_due_period()
    {
        var client = await AuthenticatedClientAsync("deposit1@example.com");
        // CreateAssignedResidentAsync agrees a deposit of rent * 2, so 10000 here.
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var response = await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(10000m, new DateOnly(2026, 1, 1)));
        var logged = await response.Content.ReadFromJsonAsync<DepositPaymentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10000m, logged!.Amount);

        var payment = Assert.Single(await StoredPaymentsAsync(residentId));
        Assert.Equal(PaymentType.Deposit, payment.Type);
        Assert.Null(payment.DuePeriodId);
        Assert.Equal(residentId, payment.ResidentId);
    }

    [Fact]
    public async Task Deposit_can_be_collected_across_multiple_payments()
    {
        var client = await AuthenticatedClientAsync("deposit2@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(4000m, new DateOnly(2026, 1, 1)));
        var second = await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(6000m, new DateOnly(2026, 2, 1)));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var payments = await StoredPaymentsAsync(residentId);
        Assert.Equal(2, payments.Count);
        Assert.Equal(10000m, payments.Sum(p => p.Amount));
    }

    [Fact]
    public async Task Deposit_payment_is_rejected_when_no_deposit_is_agreed()
    {
        var client = await AuthenticatedClientAsync("deposit3@example.com");

        // Created but never assigned to a cot, so terms - including the agreed
        // deposit - were never set.
        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Unassigned", "9999999999", "123412341234", new DateOnly(2026, 1, 1)));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        var response = await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/deposit-payments",
            new LogDepositPaymentRequest(1000m, new DateOnly(2026, 1, 1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("No deposit is agreed", await response.Content.ReadAsStringAsync());
        Assert.Empty(await StoredPaymentsAsync(resident.Id));
    }

    [Fact]
    public async Task Deposit_over_collection_is_rejected_and_leaves_the_collected_total_unchanged()
    {
        var client = await AuthenticatedClientAsync("deposit4@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(6000m, new DateOnly(2026, 1, 1)));

        // 6000 already collected against an agreed 10000, so 5000 is 1000 too far.
        var response = await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(5000m, new DateOnly(2026, 2, 1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("4000", await response.Content.ReadAsStringAsync());

        var payment = Assert.Single(await StoredPaymentsAsync(residentId));
        Assert.Equal(6000m, payment.Amount);
    }

    [Fact]
    public async Task Logging_a_deposit_does_not_settle_or_clear_rent_due_periods()
    {
        var client = await AuthenticatedClientAsync("deposit5@example.com");
        // Two months back, so the first period is already overdue and a later
        // one is merely unpaid.
        var pastMoveIn = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2));
        var (residentId, _) = await CreateAssignedResidentAsync(client, pastMoveIn, 5000m, BillingCycle.Monthly);

        var before = await (await client.GetAsync($"/api/residents/{residentId}/dues"))
            .Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        Assert.Contains(before!, d => d.IsOverdue);

        await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(10000m, DateOnly.FromDateTime(DateTime.UtcNow)));

        var after = await (await client.GetAsync($"/api/residents/{residentId}/dues"))
            .Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);

        Assert.Equal(before!.Count, after!.Count);
        foreach (var (b, a) in before.Zip(after))
        {
            Assert.Equal(b.AmountPaid, a.AmountPaid);
            Assert.Equal(b.IsPaid, a.IsPaid);
            Assert.Equal(b.IsOverdue, a.IsOverdue);
        }

        Assert.All(after, d => Assert.Equal(0m, d.AmountPaid));
        Assert.All(after, d => Assert.False(d.IsPaid));
    }

    [Fact]
    public async Task Deposit_can_be_logged_before_any_due_period_is_materialized()
    {
        var client = await AuthenticatedClientAsync("deposit6@example.com");
        // Moving in today, and dues are never read, so the materializer has not
        // created a single period yet.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (residentId, _) = await CreateAssignedResidentAsync(client, today, 5000m, BillingCycle.Monthly);

        var response = await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(10000m, today));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.DuePeriods.IgnoreQueryFilters().Where(d => d.ResidentId == residentId).ToListAsync());
    }

    [Fact]
    public async Task Deposit_payments_are_listed_for_a_resident()
    {
        var client = await AuthenticatedClientAsync("deposit7@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(4000m, new DateOnly(2026, 1, 1)));
        await client.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(6000m, new DateOnly(2026, 2, 1)));

        var response = await client.GetAsync($"/api/residents/{residentId}/deposit-payments");
        var payments = await response.Content.ReadFromJsonAsync<List<DepositPaymentResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, payments!.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), payments[0].PaidDate);
        Assert.Equal(4000m, payments[0].Amount);
        Assert.Equal(new DateOnly(2026, 2, 1), payments[1].PaidDate);
        Assert.Equal(6000m, payments[1].Amount);
    }

    [Fact]
    public async Task Resident_with_no_deposit_payments_lists_none()
    {
        var client = await AuthenticatedClientAsync("deposit8@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var response = await client.GetAsync($"/api/residents/{residentId}/deposit-payments");
        var payments = await response.Content.ReadFromJsonAsync<List<DepositPaymentResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(payments!);
    }

    [Fact]
    public async Task Rent_payments_are_not_listed_as_deposit_payments()
    {
        var client = await AuthenticatedClientAsync("deposit9@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var dues = await (await client.GetAsync($"/api/residents/{residentId}/dues"))
            .Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        await client.PostAsJsonAsync($"/api/dues/{dues!.First().Id}/payments",
            new LogPaymentRequest(5000m, new DateOnly(2026, 1, 5)));

        var payments = await (await client.GetAsync($"/api/residents/{residentId}/deposit-payments"))
            .Content.ReadFromJsonAsync<List<DepositPaymentResponse>>(TestJson.Options);

        Assert.Empty(payments!);
    }

    [Fact]
    public async Task Unpaid_due_period_past_its_due_date_is_marked_overdue()
    {
        var client = await AuthenticatedClientAsync("billing4@example.com");
        var pastMoveIn = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2));
        var (residentId, _) = await CreateAssignedResidentAsync(client, pastMoveIn, 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);

        Assert.True(dues!.First().IsOverdue);
    }
}
