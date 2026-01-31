using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DataLayer.API.Examples.IntegrationTests.TestBase;

/// <summary>
/// Custom WebApplicationFactory for integration tests that configures the testing environment.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add the Testing configuration
            config.AddJsonFile("appsettings.Testing.json", optional: false);
        });
    }
}
