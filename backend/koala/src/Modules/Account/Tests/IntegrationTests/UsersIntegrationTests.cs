using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace koala.src.Modules.Account.Tests.IntegrationTests;

// WebApplicationFactory<Program> uruchamia Twój webserwer w tle w pamięci RAM
public class AccountIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Tworzymy klienta HTTP zdolnego do odpytywania endpointów aplikacji
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_NonExistentEndpoint_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/koala/account/users");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}