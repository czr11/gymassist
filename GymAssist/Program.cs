using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using GymAssist.Controllers;
using GymAssist.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Persist data protection keys so antiforgery tokens survive restarts.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetApplicationName("GymAssist");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<CheckInOptions>(builder.Configuration.GetSection("CheckIn"));
var connectionString = BuildConnectionString(builder.Configuration);
builder.Services.AddDbContext<GymAssistDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

// Configure forwarded headers for proxies (Render, nginx, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Use forwarded headers middleware (must be before authentication)
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Only use HSTS if we're not behind a proxy
    if (!app.Environment.IsDevelopment() && !app.Configuration.GetValue<bool>("Kestrel:DisableHsts"))
    {
        app.UseHsts();
    }
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var hostname = configuration["hostname"];
    var database = configuration["dbname"];
    var username = configuration["dbuser"];
    var password = configuration["dbpass"];

    if (!string.IsNullOrWhiteSpace(hostname) &&
        !string.IsNullOrWhiteSpace(database) &&
        !string.IsNullOrWhiteSpace(username) &&
        !string.IsNullOrWhiteSpace(password))
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = hostname,
            Port = 5432,
            Database = database,
            Username = username,
            Password = password
        };

        return connectionBuilder.ConnectionString;
    }

    return configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se encontró configuración de conexión PostgreSQL.");
}
