using System.Net;
using System.Net.Http.Json;
using koala.src.Modules.Account.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace koala.src.Modules.Account.Tests.IntegrationTests;

public class AccountIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountIntegrationTests(WebApplicationFactory<Program> factory)
    {
        factory.StartServer();
        _client = factory.CreateClient();
    }

    // First test
    [Fact]
    public async Task Get_NonExistentEndpoint_ReturnsNotFound()
    {
        // Body
        LoginRequestDto body = new LoginRequestDto
        (
            "admin",
            "admin"
        );
        // Act
        var response = await _client.PostAsJsonAsync("api/koala/account/sessions", body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}