using Application.Common.Interfaces;
using Infrastructure.Configurations;
using Infrastructure.Constants.Localization;
using Infrastructure.Identity;
using Infrastructure.Middlewares;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    /// <summary>
    /// DependencyInjection class
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("VecoAvempaceDb")
                );
                // If needed: enable workflow core, etc.
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(
                        configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                );
                services.AddDatabaseDeveloperPageExceptionFilter();
                // If needed: enable workflow core, etc.
            }

            // Configure cookie policy
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Current user + domain event services
            services.AddSingleton<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IDomainEventService, DomainEventService>();

            // ---------------------------------------------------------
            // REMOVE or COMMENT OUT the extra AddIdentity calls here!
            // services.AddIdentity<ApplicationUser, ApplicationRole>()
            //     .AddEntityFrameworkStores<ApplicationDbContext>()
            //     .AddDefaultTokenProviders();
            // ---------------------------------------------------------

            // You can keep or remove duplicated cookie config
            // if it doesn't conflict with Program.cs
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // DateTime / mail services, etc.
            services.AddTransient<IDateTime, DateTimeService>();
            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.AddTransient<IMailService, MailService>();

            // If you want custom claims or user factories
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsIdentityFactory>();

            // Configure Identity options if you want (or move to Program.cs)
            services.AddAuthentication();

            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            });

            services.Configure<DataProtectionTokenProviderOptions>(opt =>
                opt.TokenLifespan = TimeSpan.FromHours(2));

            // Additional authorization policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("CanPurge", policy => policy.RequireRole("Administrator"));
                // etc...
            });

            // Localization
            services.AddLocalization(options => options.ResourcesPath = LocalizationConstants.ResourcesPath);

            // Add your custom ExceptionMiddleware, SignalR, etc.
            services.AddScoped<ExceptionMiddleware>();
            services.AddControllers();
            services.AddSignalR();

            // Configure application cookie (if not done in Program.cs)
            services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.SlidingExpiration = true;
                options.LoginPath = "/Index";
                options.LogoutPath = "/Index";
                options.AccessDeniedPath = "/Index";
            });

            return services;
        }
    }
}
