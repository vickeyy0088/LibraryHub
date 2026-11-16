using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;

namespace IntegrationTests.Controllers
{
    public class SystemControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public SystemControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // Helper method to create an HttpClient with a specific role (or null for anonymous)
        private HttpClient CreateClientWithRole(string? role)
        {
            var client = _factory.CreateClient();

            if (role != null)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
                client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            }

            return client;
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthyStatus()
        {
            // Arrange
            var client = CreateClientWithRole(null);

            // Act
            var response = await client.GetAsync("/api/system/health", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            content.Should().Contain("System is healthy");
        }
    }
}
