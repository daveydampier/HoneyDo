using HoneyDo.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDo.Tests.Fixtures;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove both the resolved options AND EF Core's stored configuration actions
            // (which include UseSqlite) — otherwise both providers conflict at startup.
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Capture the name BEFORE the options lambda — Guid.NewGuid() inside the
            // lambda would run on every DbContext resolution, giving each request its
            // own empty in-memory database and making tests unable to see each other's data.
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });

        builder.UseSetting("Jwt:Key", "TestSecret-MinLength-32Characters-ForTests!!");
        builder.UseSetting("Jwt:Issuer", "HoneyDo");
        builder.UseSetting("Jwt:Audience", "HoneyDo");
        builder.UseSetting("Jwt:ExpiryHours", "1");
        builder.UseSetting("AllowedOrigins", "http://localhost:5173");
    }

    public async Task InitializeAsync()
    {
        // Accessing Services triggers lazy host initialization; EnsureCreated then
        // applies HasData seed rows (TaskStatus) to the InMemory database.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();
}
