using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using UniNest.Application;

namespace UniNest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        if (connectionString.Contains(".db") || (connectionString.Contains("Data Source=") && !connectionString.Contains("Server=")))
        {
            services.AddDbContext<UniNestDbContext>(options => options.UseSqlite(connectionString));
        }
        else
        {
            services.AddDbContext<UniNestDbContext>(options => options.UseSqlServer(connectionString));
        }

        services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            // ── C-5 KNOWN GAP (Security) ──────────────────────────────────────
            // Email confirmation is intentionally DISABLED because no IEmailSender
            // is implemented yet (ForgotPassword is also a stub — see AuthService).
            // Enabling RequireConfirmedEmail = true without email delivery would
            // permanently lock out all users who register via the API.
            //
            // ACTION REQUIRED before production:
            //   1. Implement IEmailSender (e.g., via SendGrid or SMTP)
            //   2. Call userManager.SendEmailConfirmationAsync in AuthService.RegisterAsync
            //   3. Set options.SignIn.RequireConfirmedEmail = true here
            //   4. Delete all unconfirmed users created during development
            // ─────────────────────────────────────────────────────────────────
            options.SignIn.RequireConfirmedEmail = false; // TODO: set true when email is implemented
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<UniNestDbContext>()
        .AddSignInManager<SignInManager<AppUser>>()
        .AddDefaultTokenProviders();

        // ── JWT ──────────────────────────────────────────────────────────────
        // Read configuration ONCE and fail fast if missing.
        // Both IOptions<JwtOptions> (injected into JwtTokenService) and
        // AddJwtBearer (token validation middleware) must use the SAME binding
        // to avoid the split-state bug where generation and validation use
        // different keys.
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{JwtOptions.SectionName}' is required and must contain a valid Secret.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
            throw new InvalidOperationException(
                $"'{JwtOptions.SectionName}:Secret' must not be empty. " +
                "Set it via environment variable Jwt__Secret or dotnet user-secrets.");

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IInquiryService, InquiryService>();
        services.AddScoped<IFavoriteService, FavoriteService>();

        // ── Email service ──────────────────────────────────────────────────
        // Select implementation based on configuration:
        // - SmtpEmailService when Email:SmtpHost is configured
        // - NullEmailService when Email:SmtpHost is not set
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>();

        if (!string.IsNullOrWhiteSpace(emailOptions?.SmtpHost))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddLogging(logging =>
                logging.AddFilter("UniNest.Infrastructure.SmtpEmailService", LogLevel.Information));
        }
        else
        {
            services.AddScoped<IEmailService, NullEmailService>();
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireStudentRole", policy => policy.RequireRole("Student"))
            .AddPolicy("RequireOwnerRole", policy => policy.RequireRole("Owner"))
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

        return services;
    }
}
