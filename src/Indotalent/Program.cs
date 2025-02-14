using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// Add services to the container.
builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
{
    // This maps the root URL "/" to the Produits page.
    options.Conventions.AddPageRoute("/Produits", "");
});

// Ajouter les services de session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Durée de la session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSession(); // Activer les sessions

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
