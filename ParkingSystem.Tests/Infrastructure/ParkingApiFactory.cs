using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ParkingSystem.Repositories;
using ParkingSystem.Services;   // adjust

namespace ParkingSystem.Tests.Infrastructure;

public class ParkingApiFactory : WebApplicationFactory<Program>
{
    public Mock<IParkingRepository> Repo { get; } = new();
    public Mock<IFeeCalculator> Fee { get; } = new();
    public Mock<IParkingSpaceTracker> Tracker { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs reads these at startup; User Secrets don't load in "Testing"
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-1234567890");
        builder.UseSetting("Jwt:Issuer", "test-issuer");
        builder.UseSetting("Jwt:Audience", "test-audience");

        builder.ConfigureTestServices(services =>
        {
            // Swap the real dependencies for mocks
            services.RemoveAll<IParkingRepository>();
            services.AddSingleton(Repo.Object);

            services.RemoveAll<IFeeCalculator>();
            services.AddSingleton(Fee.Object);

            services.RemoveAll<IParkingSpaceTracker>();
            services.AddSingleton(Tracker.Object);

            // Swap JWT for the fake login handler
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public void ResetMocks()
    {
        Repo.Reset();
        Fee.Reset();
        Tracker.Reset();
    }

    // role = "Admin", "User", or null for an anonymous caller
    public HttpClient CreateClientAs(string? role)
    {
        var client = CreateClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        }
        return client;
    }
}