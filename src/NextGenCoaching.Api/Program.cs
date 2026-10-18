using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using NextGenCoaching.Api.Authentication;
using NextGenCoaching.Api.Endpoints;
using NextGenCoaching.Api.Middleware;
using NextGenCoaching.Api.Validation;
using NextGenCoaching.Core;
using NextGenCoaching.Core.Interfaces;
using NextGenCoaching.Core.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local development secrets and an isolated database, created by Start-LocalBackend.ps1.
// Explicit environment variables and command-line settings still take precedence.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .AddJsonFile(Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../.local/backend.json")), optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}

// ---------- Logging (Serilog) ----------
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------- Core services (DbContext, Identity, JWT, services) ----------
builder.Services.AddCore(builder.Configuration);

// ---------- Razorpay + payments ----------
builder.Services.Configure<NextGenCoaching.Core.Payments.RazorpayOptions>(
    builder.Configuration.GetSection(NextGenCoaching.Core.Payments.RazorpayOptions.SectionName));
builder.Services.AddHttpClient<NextGenCoaching.Core.Payments.RazorpayClient>();
builder.Services.AddScoped<NextGenCoaching.Core.Payments.PaymentService>();

// ---------- Current user (from HttpContext) ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---------- CORS for the Blazor Web App ----------
var webOrigin = builder.Configuration["Cors:WebOrigin"] ?? "https://localhost:7100";
builder.Services.AddCors(o => o.AddPolicy("Web", p => p
    .WithOrigins(webOrigin, "http://localhost:5100", "https://localhost:7100")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NextGen Coaching API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT here (without the 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }]
            = Array.Empty<string>()
    });
});

// ---------- Rate limiting on public POST endpoints ----------
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddPolicy("public-post", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opts.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

// ---------- Pipeline ----------
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NextGen API v1"));
}

var storageSection = builder.Configuration["Storage:LocalRoot"];
var uploadRoot = !string.IsNullOrWhiteSpace(storageSection)
    ? storageSection
    : Path.Combine(AppContext.BaseDirectory, "uploads");
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads"
});

app.UseCors("Web");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Validate the shared request DTOs on every API route.
var api = app.MapGroup(string.Empty).AddEndpointFilter<RequestValidationFilter>();
api.MapPublicEndpoints();
api.MapAuthEndpoints();
api.MapAdminLeadsEndpoints();
api.MapAdminWebinarsEndpoints();
api.MapAdminDashboardEndpoints();
api.MapAdminPaymentEndpoints();
api.MapAdminCoursesEndpoints();
api.MapAdminBatchesEndpoints();
api.MapAdminInstructorsEndpoints();
api.MapAdminSettingsEndpoints();
api.MapAdminStudentsEndpoints();
api.MapAdminPlacementsEndpoints();
api.MapAdminCurriculumEndpoints();
api.MapAdminMockInterviewsEndpoints();
api.MapAdminReportsEndpoints();
api.MapAdminSearchEndpoints();
api.MapAdminUsersEndpoints();
api.MapStudentEndpoints();
api.MapInstructorEndpoints();
api.MapHrEndpoints();
api.MapPaymentEndpoints();
app.MapHealthEndpoints();

// ---------- Seed on startup ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seeding failed");
        throw new InvalidOperationException("Backend startup failed: the database could not be initialized. Check the database connection and startup log.", ex);
    }
}

app.Run();
