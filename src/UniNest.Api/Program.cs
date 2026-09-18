using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;
using UniNest.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Global file upload size limits (C-2) ─────────────────────────────────────
// Enforced at the HTTP layer before the request even reaches the controller.
const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB
builder.WebHost.ConfigureKestrel(k =>
    k.Limits.MaxRequestBodySize = MaxUploadBytes);
builder.Services.Configure<FormOptions>(o =>
    o.MultipartBodyLengthLimit = MaxUploadBytes);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            title = "Too Many Requests",
            detail = "You have exceeded the rate limit. Please wait a moment and try again."
        }, cancellationToken: token);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 120, // 120 requests per minute per IP
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 10
            });
    });
});

builder.Services.AddHealthChecks().AddDbContextCheck<UniNestDbContext>("database");
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "UniNest API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token in the text input below."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS (C-3) ────────────────────────────────────────────────────────────────
// Never use AllowAnyOrigin() in production — it bypasses browser same-origin
// protection. In Development we allow common local frontend dev servers.
// In production, set the allowed origins in configuration:
//   "Cors:AllowedOrigins": ["https://app.uninest.com"]
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins != null && origins.Length > 0)
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// ── Middleware pipeline (correct order) ───────────────────────────────────────
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
    await Results.Problem(statusCode: 500, title: "An unexpected error occurred.").ExecuteAsync(context);
}));

app.UseHttpsRedirection();  // Must be before CORS and static files

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var parentDir = Directory.GetParent(builder.Environment.ContentRootPath)?.Parent?.FullName ?? builder.Environment.ContentRootPath;
var wwwrootDir = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(wwwrootDir);

var compositeProvider = new Microsoft.Extensions.FileProviders.CompositeFileProvider(
    new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootDir),
    new Microsoft.Extensions.FileProviders.PhysicalFileProvider(parentDir)
);

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = compositeProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = compositeProvider });

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services);
app.Run();

public partial class Program;
