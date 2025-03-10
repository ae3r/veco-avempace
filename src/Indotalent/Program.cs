using Application;
using Infrastructure;
using Infrastructure.Services.Ocpp;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// Add Razor Pages and map the root URL "/" to the Login page
builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
{
    options.Conventions.AddPageRoute("/Login", "");
});

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session duration
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register the OCPP service for dependency injection
builder.Services.AddSingleton<IOcppService, OcppService>();

var app = builder.Build();

// Use configured localization
app.UseRequestLocalization(
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value
);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession(); // Enable sessions

app.UseRouting();

// Enable WebSockets (this must be before mapping WebSocket endpoints)
app.UseWebSockets();

// (Optional) For testing only: override the Host header if needed
/*app.Use(async (context, next) =>
{
    context.Request.Host = new Microsoft.AspNetCore.Http.HostString("localhost");
    await next();
});*/

app.UseAuthorization();

// Map the OCPP WebSocket endpoint
app.Map("/ocpp", async context =>
{
    var ocppService = context.RequestServices.GetRequiredService<IOcppService>();
    await ocppService.ProcessWebSocketAsync(context);
});

// Map Razor Pages
app.MapRazorPages();

app.Run();
