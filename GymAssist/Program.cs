using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using GymAssist.Controllers;
using GymAssist.Data;
using GymAssist.Security;

using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Persist data protection keys so antiforgery tokens survive restarts.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetApplicationName("GymAssist");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "GymAssist.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<AesPasswordService>();
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
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    context.Response.Headers["X-Frame-Options"] = "DENY";

    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        return databaseUrl;
    }

    var hostname = GetEnvironmentValue("hostname", "DB_HOST");
    var database = GetEnvironmentValue("dbname", "DB_NAME");
    var username = GetEnvironmentValue("dbuser", "DB_USER");
    var password = GetEnvironmentValue("dbpass", "DB_PASSWORD");

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

    if (configuration["ASPNETCORE_ENVIRONMENT"] == "Production")
    {
        var missingVariables = new[]
        {
            (Name: "hostname", Value: hostname),
            (Name: "dbname", Value: database),
            (Name: "dbuser", Value: username),
            (Name: "dbpass", Value: password)
        }
        .Where(variable => string.IsNullOrWhiteSpace(variable.Value))
        .Select(variable => variable.Name);

        throw new InvalidOperationException(
            $"Faltan variables de entorno PostgreSQL en Render: {string.Join(", ", missingVariables)}.");
    }

    return configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se encontró configuración de conexión PostgreSQL.");
}

static string? GetEnvironmentValue(string name, string alternateName)
{
    return Environment.GetEnvironmentVariable(name)
    ?? Environment.GetEnvironmentVariable(alternateName)
    ?? Environment.GetEnvironmentVariable(alternateName.ToLowerInvariant());
}
