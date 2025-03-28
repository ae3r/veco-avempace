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

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Set to Debug to capture all messages
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File("/var/www/avemplace/logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["AllowedHosts"] = builder.Configuration["AllowedHosts"] ?? "*";

// 1) Configure EF Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Add Identity here -- only once
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // optional password/sign-in settings
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3) Add your Infrastructure/Application layers
// IMPORTANT: Make sure AddInfrastructure(...) does NOT also call AddIdentity again
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// 4) Add Razor Pages with custom routing
builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
{
    options.Conventions.AddPageRoute("/Login", "");
});

// 5) Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 6) Register OCPP + ChargingStation services
builder.Services.AddScoped<IOcppService, OcppService>();
builder.Services.AddScoped<IChargingStationService, ChargingStationService>();

// 7) Localization
var supportedCultures = new[] { new CultureInfo("fr"), new CultureInfo("en") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fr");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Already added above, but we can add again if needed
builder.Services.AddRazorPages();

var app = builder.Build();

// 8) Migrate & Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Migrate automatically if you want
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // Seed roles
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var adminRole = new ApplicationRole { Name = "Admin",
                Description = "Administrator role"};
            await roleManager.CreateAsync(adminRole);
        }

        // Seed admin user
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = "admin@myapp.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                // Required by IdentityUser (for login)
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,

                // Non-null columns (example properties) in your ApplicationUser
                DisplayName = "Administrator",
                Site = "DefaultSite",
                ProfilePictureDataUrl = "https://example.com/default-profile-pic.png",
                IsActive = true,
                IsLive = true,
                RefreshToken = Guid.NewGuid().ToString("N"), // or "placeholder"
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30), // or any future date
                // ...
            };

            await userManager.CreateAsync(adminUser, "AdminPassword123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    catch (Exception ex)
    {
        // Log or handle errors
        Console.WriteLine(ex);
    }
}

// 9) Request Localization
app.UseRequestLocalization(app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>()
    .Value);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 10) Pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

// Note: Also use app.UseAuthentication(); if needed
// (e.g. app.UseAuthentication(); THEN app.UseAuthorization();)
app.UseAuthorization();

// 11) Enable WebSockets
app.UseWebSockets();

// 12) OCPP endpoint
app.Map("/ocpp/{stationId}", async context =>
{
    try
    {
        var stationId = context.Request.RouteValues["stationId"]?.ToString() ?? "";
        var ocppService = context.RequestServices.GetRequiredService<IOcppService>();
        await ocppService.ProcessWebSocketAsync(context, stationId);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in OCPP endpoint: {ex}");
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
        }
    }
});

// 13) Optional test endpoint
app.MapGet("/trigger/{stationId}", async (HttpContext context, string stationId, IOcppService ocppService) =>
{
    await ocppService.SendTriggerMessageAsync(context, stationId, "BootNotification");
    return Results.Ok($"Trigger message for {stationId} sent.");
});

// 14) Razor Pages
app.MapRazorPages();

app.Run();
