using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;

namespace HostelManagement.Server.Tests;

public class AccountTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Owner_can_complete_setup_and_immediately_create_a_hostel()
    {
        var client = _factory.CreateClient();

        var setupResponse = await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Pune Hostels", "owner1@example.com", "P@ssw0rd!"));
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        var auth = await setupResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));

        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.Token);
        var createHostelResponse = await client.PostAsJsonAsync("/api/hostels",
            new CreateHostelRequest("Hostel One", "Pune"));

        Assert.Equal(HttpStatusCode.Created, createHostelResponse.StatusCode);
    }

    [Fact]
    public async Task Invalid_login_credentials_are_rejected_without_creating_a_session()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Pune Hostels", "owner2@example.com", "P@ssw0rd!"));

        var loginResponse = await client.PostAsJsonAsync("/api/account/login",
            new LoginRequest("owner2@example.com", "WrongPassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Valid_login_issues_a_token_scoped_to_the_owners_org()
    {
        var client = _factory.CreateClient();

        var setupResponse = await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Pune Hostels", "owner3@example.com", "P@ssw0rd!"));
        var setupAuth = await setupResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var loginResponse = await client.PostAsJsonAsync("/api/account/login",
            new LoginRequest("owner3@example.com", "P@ssw0rd!"));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(setupAuth!.OwnerOrgId, loginAuth!.OwnerOrgId);
    }

    [Fact]
    public async Task Cross_org_access_is_denied_and_does_not_leak_existence()
    {
        var client = _factory.CreateClient();

        var ownerASetup = await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Org A", "ownerA@example.com", "P@ssw0rd!"));
        var ownerAAuth = await ownerASetup.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", ownerAAuth!.Token);
        var createHostelResponse = await client.PostAsJsonAsync("/api/hostels",
            new CreateHostelRequest("Org A Hostel", "Pune"));
        var ownerAHostel = await createHostelResponse.Content.ReadFromJsonAsync<HostelResponse>();

        var ownerBSetup = await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Org B", "ownerB@example.com", "P@ssw0rd!"));
        var ownerBAuth = await ownerBSetup.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", ownerBAuth!.Token);
        var getResponse = await client.GetAsync($"/api/hostels/{ownerAHostel!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
