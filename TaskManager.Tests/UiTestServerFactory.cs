using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManager.Api.Data;

namespace TaskManager.Tests;

// Pokreće pravu aplikaciju na stvarnom lokalnom portu (Kestrel), da je browser može otvoriti.
// Koristi privremenu bazu, pa se taskmanager.db nikad ne dira.
public class UiTestServerFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"taskmanager-ui-{Guid.NewGuid():N}.db");

    private IHost? _kestrelHost;

    // Adresa na kojoj aplikacija sluša, npr. http://127.0.0.1:54321
    public string ServerAddress { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}"
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // 1) Standardni (in-memory) host koji WebApplicationFactory očekuje
        var testHost = builder.Build();

        // 2) Pravi host na Kestrelu, na slobodnom portu (port 0 = sistem sam bira)
        builder.ConfigureWebHost(webBuilder =>
            webBuilder.UseKestrel(options => options.Listen(IPAddress.Loopback, 0)));
        _kestrelHost = builder.Build();

        using (var scope = _kestrelHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Zaštita: testovi nikad ne smiju dirati pravu bazu
            if (db.Database.GetConnectionString()?.Contains(_dbPath) != true)
                throw new InvalidOperationException("UI testovi nisu preusmjereni na privremenu bazu.");

            db.Database.EnsureCreated();
        }

        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        ServerAddress = server.Features.Get<IServerAddressesFeature>()!.Addresses.First();

        testHost.Start();
        return testHost;
    }

    // Briše sve zadatke, da svaki UI test krene od prazne liste
    public void ResetDatabase()
    {
        using var scope = _kestrelHost!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM \"Tasks\"");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.StopAsync().GetAwaiter().GetResult();
            _kestrelHost?.Dispose();
        }

        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(_dbPath); } catch (IOException) { }
        }
    }
}