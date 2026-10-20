using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextGenCoaching.Web.Components;
using NextGenCoaching.Web.Services;
using Serilog;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);
builder.AddDeploymentServices();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AssetVersions>();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// ---------- Logging ----------
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------- Blazor ----------
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------- Auth state (scoped per circuit, persisted via ProtectedLocalStorage) ----------
// AddInteractiveServerComponents registers ProtectedLocalStorage; TryAdd keeps us safe if that changes.
builder.Services.TryAddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<SitePreferences>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ---------- Typed HTTP client pointed at the Api ----------
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5101";

// Handler-free named client used ONLY by TokenRefreshService for POST /auth/refresh.
// It must bypass AuthorizationMessageHandler, otherwise a refresh would recurse into itself.
builder.Services.AddHttpClient(TokenRefreshService.HttpClientName, c =>
{
    c.BaseAddress = new Uri(apiBaseUrl);
    c.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient("backend-readiness", c => { c.BaseAddress = new Uri(apiBaseUrl); c.Timeout = TimeSpan.FromSeconds(4); });

// One shared connection pool for all circuits (replaces IHttpClientFactory's pooling).
builder.Services.AddSingleton(_ => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});

// ApiClient is built manually per circuit so AuthorizationMessageHandler sees THIS circuit's
// AuthStateService. Do NOT register the handler via AddHttpMessageHandler: IHttpClientFactory
// resolves handlers in its own DI scope and caches the pipeline across circuits, which detaches
// it from circuit-scoped auth state (and could share one user's state with another).
builder.Services.AddScoped(sp =>
{
    var authHandler = new AuthorizationMessageHandler(
        sp.GetRequiredService<AuthStateService>(),
        sp.GetRequiredService<TokenRefreshService>())
    {
        InnerHandler = sp.GetRequiredService<SocketsHttpHandler>()
    };

    // disposeHandler: false — the SocketsHttpHandler at the bottom of the chain is a shared singleton.
    var http = new HttpClient(authHandler, disposeHandler: false)
    {
        BaseAddress = new Uri(apiBaseUrl),
        Timeout = TimeSpan.FromSeconds(20)
    };

    var proxyKey = builder.Configuration["Api:ProxyKey"];
    var caller = sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Connection.RemoteIpAddress;
    if (!string.IsNullOrWhiteSpace(proxyKey) && caller is not null)
    {
        http.DefaultRequestHeaders.Add("X-NextGen-Proxy-Key", proxyKey);
        http.DefaultRequestHeaders.Add("X-NextGen-Client-IP", caller.ToString());
    }
    return new ApiClient(http, sp.GetRequiredService<ILogger<ApiClient>>());
});

// ---------- API client wrappers ----------
builder.Services.AddScoped<CourseApiClient>();
builder.Services.AddScoped<WebinarApiClient>();
builder.Services.AddScoped<LeadApiClient>();
builder.Services.AddScoped<ContentApiClient>();
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<StudentApiClient>();
builder.Services.AddScoped<InstructorApiClient>();
builder.Services.AddScoped<HrApiClient>();
builder.Services.AddScoped<PaymentApiClient>();

var app = builder.Build();
app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'; object-src 'none'; base-uri 'self'";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    if (builder.Configuration.GetValue("Hosting:HttpsRedirect", true)) app.UseHttpsRedirection();
}
// Compress public assets only: authenticated HTML, API responses and SignalR remain outside this branch.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/css")
    || context.Request.Path.StartsWithSegments("/js")
    || context.Request.Path.StartsWithSegments("/images")
    || context.Request.Path.StartsWithSegments("/_framework"), branch => branch.UseResponseCompression());
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Only fingerprinted files are immutable. Other assets revalidate after an hour.
        context.Context.Response.Headers.CacheControl = context.Context.Request.Query.ContainsKey("v")
            ? "public,max-age=31536000,immutable"
            : "public,max-age=3600";
    }
});
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseAntiforgery();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (IHttpClientFactory clients) =>
{
    try
    {
        using var response = await clients.CreateClient("backend-readiness").GetAsync("/health/ready");
        return response.IsSuccessStatusCode ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503);
    }
    catch (HttpRequestException) { return Results.StatusCode(503); }
    catch (TaskCanceledException) { return Results.StatusCode(503); }
});
app.MapGet("/robots.txt", (IConfiguration configuration) => Results.Text(configuration.GetValue<bool>("Site:Indexable")
    ? "User-agent: *\nDisallow: /admin\nDisallow: /student\nDisallow: /instructor\nDisallow: /hr\nDisallow: /enroll\nDisallow: /checkout\nDisallow: /login\nDisallow: /signup\n"
    : "User-agent: *\nDisallow: /\n", "text/plain"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
