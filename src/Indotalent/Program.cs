using Application;
using Application.Common.Interfaces;
using Infrastructure;
using Infrastructure.Ocpp;
using Infrastructure.Services;
using Infrastructure.Services.Ocpp;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using Serilog;
using System.Globalization;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.Extensions.Options;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File("/var/www/avemplace/logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Configuration["AllowedHosts"] ??= "*";

// Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/www/avemplace/keys"))
    .SetApplicationName("Avemplace");

// Kestrel HTTPS (app listens on 5002; proxy 443->5002 must be configured)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, listenOptions =>
    {
        var certPath = "/etc/letsencrypt/live/avemplace.com/avemplace.pfx";
        var certPassword = "Avempace2025!";
        var serverCert = new X509Certificate2(certPath, certPassword);
        Log.Information("Kestrel binding port 5002 with Subject={Subject}", serverCert.Subject);
        listenOptions.UseHttps(serverCert);
        Log.Information("Kestrel HTTPS configured on port 5002");
    });
});

// EF Core
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Layers
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// Razor Pages
builder.Services.AddRazorPages().AddRazorPagesOptions(opts =>
{
    opts.Conventions.AddPageRoute("/Login", "");
});

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opts =>
{
    opts.IdleTimeout = TimeSpan.FromMinutes(30);
    opts.Cookie.HttpOnly = true;
    opts.Cookie.IsEssential = true;
});

// OCPP services
builder.Services.AddScoped<IOcppService, OcppService>();
builder.Services.AddScoped<IChargingStationService, ChargingStationService>();
builder.Services.AddHostedService<OcppConfigurationService>();

// Localization
var supportedCultures = new[] { new CultureInfo("fr"), new CultureInfo("en") };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new RequestCulture("fr");
    opts.SupportedCultures = supportedCultures;
    opts.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Migrate & Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    var roleMgr = services.GetRequiredService<RoleManager<ApplicationRole>>();
    if (!await roleMgr.RoleExistsAsync("Admin"))
    {
        await roleMgr.CreateAsync(new ApplicationRole { Name = "Admin", Description = "Administrator role" });
    }

    var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = "admin@myapp.com";
    if (await userMgr.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator",
            Site = "DefaultSite",
            ProfilePictureDataUrl = "https://example.com/default-profile-pic.png",
            IsActive = true,
            IsLive = true,
            RefreshToken = Guid.NewGuid().ToString("N"),
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30)
        };
        await userMgr.CreateAsync(admin, "AdminPassword123!");
        await userMgr.AddToRoleAsync(admin, "Admin");
    }
}

// Localization
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

// Keep /ocpp free of anti-forgery/redirect side-effects if you add any later
app.Use(async (context, next) =>
{
    // If you add middleware that could interfere with WS, keep this pattern.
    await next();
});

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(20), // send ping every 20s
    ReceiveBufferSize = 8192
});


// OCPP endpoint
app.Map("/ocpp/{stationId}", async (HttpContext context) =>
{
    var stationId = context.Request.RouteValues["stationId"]?.ToString() ?? string.Empty;
    var ocpp = context.RequestServices.GetRequiredService<IOcppService>();
    await ocpp.ProcessWebSocketAsync(context, stationId);
});

// Trigger endpoint
app.MapGet("/trigger/{stationId}", async (string stationId, IOcppService ocppService) =>
{
    await ocppService.SendTriggerMessageAsync(stationId, "MeterValues");
    return Results.Ok($"TriggerMessage(MeterValues) sent to {stationId}");
});

app.MapRazorPages();
app.Run();
