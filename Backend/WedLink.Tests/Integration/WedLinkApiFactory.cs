using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Persistence;
using WedLink.API.Services;

namespace WedLink.Tests.Integration;

public sealed class WedLinkApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"WedLinkTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WedLinkDb"] =
                    "Server=(local);Database=WedLinkTests;Integrated Security=True;",
                ["PublicSite:BaseUrl"] = "https://public.example.test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            foreach (var hostedService in services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType is not null
                && (descriptor.ImplementationType == typeof(InvitationExpirationWorker)
                    || descriptor.ImplementationType == typeof(MediaRetentionWorker))).ToArray())
                services.Remove(hostedService);

            services.RemoveAll<DbContextOptions<WedLinkDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<WedLinkDbContext>>();
            services.RemoveAll<WedLinkDbContext>();
            services.AddDbContext<WedLinkDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    public async Task CreateDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task CreateSuperAdminAsync(string email, string password)
    {
        await using var scope = Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("Could not create test role.");
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
        var user = new AdminUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException("Could not create test admin.");
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, "SuperAdmin");
        if (!roleAssignment.Succeeded)
        {
            throw new InvalidOperationException("Could not assign test admin role.");
        }
    }
}
