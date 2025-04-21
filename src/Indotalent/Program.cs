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
using IdentityModel;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Capture all messages
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File("/var/www/avemplace/logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["AllowedHosts"] = builder.Configuration["AllowedHosts"] ?? "*";

// Configure data protection to persist keys to the file system.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/www/avemplace/keys"))
    .SetApplicationName("Avemplace");

// ------------------ Certificate Loading & Kestrel Configuration ------------------

// Define the certificate file path and the PFX password.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, listenOptions =>
    {
        // 1) load the cert yourself so you can inspect it
        var certPath = "/etc/letsencrypt/live/avemplace.com/avemplace.pfx";
        var certPassword = "Avempace2025!";
        X509Certificate2 serverCert;
        try
        {
            serverCert = new X509Certificate2(certPath, certPassword);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load server cert at {CertPath}", certPath);
            throw;
        }

        // 2) log everything you want to verify
        Log.Information("Kestrel: binding port 5002 with server certificate Subject={Subject}, Thumbprint={Thumbprint}",
                        serverCert.Subject, serverCert.Thumbprint);

        // 3) hand the cert to Kestrel
        listenOptions.UseHttps(serverCert);

        // 4) optionally log right after
        Log.Information("Kestrel: UseHttps() call completed on port 5002");
    });
});

// -------------------------------------------------------------------------------
// 1) Configure EF Core with SQL Server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Add Identity (only once).
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3) Add your Infrastructure/Application layers.
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// 4) Add Razor Pages with custom routing.
builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
{
    options.Conventions.AddPageRoute("/Login", "");
});

// 5) Configure session services.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 6) Register OCPP & ChargingStation services.
builder.Services.AddScoped<IOcppService, OcppService>();
builder.Services.AddScoped<IChargingStationService, ChargingStationService>();
// IMPORTANT: The background service now injects IServiceProvider in order to create its own scope.
builder.Services.AddHostedService<OcppConfigurationService>();

// 7) Configure Localization.
var supportedCultures = new[] { new CultureInfo("fr"), new CultureInfo("en") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fr");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// (Re-)add Razor Pages if needed.
builder.Services.AddRazorPages();

var app = builder.Build();

// 8) Migrate & seed the database.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var adminRole = new ApplicationRole { Name = "Admin", Description = "Administrator role" };
            await roleManager.CreateAsync(adminRole);
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = "admin@myapp.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
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
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30),
            };

            await userManager.CreateAsync(adminUser, "AdminPassword123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Database seeding error: " + ex);
    }
}

// 9) Request Localization.
app.UseRequestLocalization(app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 10) Configure middleware pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

// Middleware to bypass antiforgery on OCPP paths.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/ocpp"))
    {
        Console.WriteLine("Bypassing antiforgery for WebSocket request: " + context.Request.Path);
        await next();
    }
    else
    {
        await next();
    }
});

app.UseWebSockets();

// 2) Enable WebSockets and map the OCPP endpoint:
app.UseWebSockets();
app.Map("/ocpp/{stationId}", async context =>
{
    var stationId = (string)context.Request.RouteValues["stationId"]!;
    var ocppService = context.RequestServices.GetRequiredService<IOcppService>();
    await ocppService.ProcessWebSocketAsync(context, stationId);
});

// 3) (Optional) Trigger endpoint to ask a charger to send a message:
app.MapGet("/trigger/{stationId}", async (string stationId, IOcppService ocppService) =>
{
    await ocppService.SendTriggerMessageAsync(stationId, "MeterValues");
    return Results.Ok($"Triggered MeterValues on {stationId}");
});


// 13) Map Razor Pages.
app.MapRazorPages();

app.Run();
