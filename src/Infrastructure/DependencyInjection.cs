using Application.Common.Interfaces;
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

namespace Infrastructure;

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

            // enable workflow core
            //services.AddWorkflow();
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))

                );
            services.AddDatabaseDeveloperPageExceptionFilter();
            // enable workflow core
            //services.AddWorkflow(x => x.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), true, true));
        }


        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });
        //services.Configure<SmartSettings>(configuration.GetSection(SmartSettings.SectionName));
        //services.AddSingleton(s => s.GetRequiredService<IOptions<SmartSettings>>().Value);
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
        services.AddScoped<IDomainEventService, DomainEventService>();

        services.AddIdentity<ApplicationUser, ApplicationRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();
        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        services.AddTransient<IDateTime, DateTimeService>();
        //services.AddTransient<IExcelService, ExcelService>();
        //services.AddTransient<IUploadService, UploadService>();
        //services.AddTransient<IIdentityService, IdentityService>();
        //services.Configure<AppConfigurationSettings>(configuration.GetSection("AppConfigurationSettings"));
        //services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
        //services.AddTransient<IMailService, SMTPMailService>();

        services.AddAuthentication();
        services.Configure<IdentityOptions>(options =>
        {
            // Default SignIn settings.
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            // Default Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        });

        services.Configure<DataProtectionTokenProviderOptions>(opt =>
                opt.TokenLifespan = TimeSpan.FromHours(2));
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanPurge", policy => policy.RequireRole("Administrator"));
            // Here I stored necessary permissions/roles in a constant
            //foreach (var prop in typeof(Permissions).GetNestedTypes().SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
            //{
            //    var propertyValue = prop.GetValue(null);
            //    if (propertyValue is not null)
            //    {
            //        options.AddPolicy(propertyValue.ToString(), policy => policy.RequireClaim(ApplicationClaimTypes.Permission, propertyValue.ToString()));
            //    }
            //}
        });
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsIdentityFactory>();
        services.AddLocalization(options => options.ResourcesPath = LocalizationConstants.ResourcesPath);
        services.AddScoped<ExceptionMiddleware>();

        services.AddControllers();
        services.AddSignalR();
       
        services.ConfigureApplicationCookie(options => {
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;
            options.LoginPath = "/Index";
            options.LogoutPath = "/Index";
            options.AccessDeniedPath = "/Index";
        });

        return services;
    }



}