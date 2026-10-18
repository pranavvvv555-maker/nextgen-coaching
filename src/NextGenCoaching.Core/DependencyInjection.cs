using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextGenCoaching.Core.Identity;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using NextGenCoaching.Core.Persistence.Interceptors;
using NextGenCoaching.Core.Services.Email;
using NextGenCoaching.Core.Services.Settings;
using NextGenCoaching.Core.Services.Sms;
using NextGenCoaching.Core.Services.Storage;

namespace NextGenCoaching.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration config)
    {
        // Audit interceptor — needs to resolve ICurrentUserService at SaveChanges time
        services.AddScoped<AuditableEntityInterceptor>();

        // DbContext — Postgres + snake_case + audit interceptor
        services.AddDbContext<AppDbContext>((sp, opts) =>
        {
            var cs = config.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured");

            opts.UseNpgsql(cs, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });
            opts.UseSnakeCaseNamingConvention();
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        // Identity — uses Guid keys, configured for our use case
        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.User.RequireUniqueEmail = true;
                o.SignIn.RequireConfirmedEmail = false; // turn on in prod once email is wired
            })
            .AddRoles<ApplicationRole>()
            .AddRoleManager<RoleManager<ApplicationRole>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT bearer auth
        var jwt = config.GetSection("Jwt");
        var secret = jwt["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"] ?? "nextgen-coaching",
                    ValidAudience = jwt["Audience"] ?? "nextgen-coaching-web",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdmin",       p => p.RequireRole(Shared.Constants.RoleNames.Admin))
            .AddPolicy("RequireCounsellor",  p => p.RequireRole(Shared.Constants.RoleNames.Counsellor, Shared.Constants.RoleNames.Admin))
            .AddPolicy("RequireInstructor",  p => p.RequireRole(Shared.Constants.RoleNames.Instructor, Shared.Constants.RoleNames.Admin))
            .AddPolicy("RequireHrReviewer",  p => p.RequireRole(Shared.Constants.RoleNames.HrReviewer, Shared.Constants.RoleNames.Admin))
            .AddPolicy("RequireStudent",     p => p.RequireRole(Shared.Constants.RoleNames.Student));

        // Token service
        services.AddScoped<ITokenService, TokenService>();

        // Email settings (dedicated email_settings table) — singleton cache + scoped reader/writer.
        services.AddSingleton<EmailSettingsCache>();
        services.AddScoped<IEmailSettingsService, EmailSettingsService>();

        // Email templating + dispatch (renders EmailTemplate rows, sends via IEmailSender).
        services.AddScoped<ITemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IEmailDispatcher, EmailDispatcher>();

        // External services — swap implementations as needed
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ISmsSender, StubSmsSender>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        // CAPTCHA (safe-by-default: inert until a SecretKey is configured)
        services.Configure<NextGenCoaching.Core.Security.CaptchaOptions>(
            config.GetSection(NextGenCoaching.Core.Security.CaptchaOptions.SectionName));
        services.AddHttpClient<ICaptchaVerifier, NextGenCoaching.Core.Security.TurnstileCaptchaVerifier>();

        return services;
    }
}
