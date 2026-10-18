using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextGenCoaching.Web.Components;
using NextGenCoaching.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
    c.Timeout = TimeSpan.FromSeconds(3);
});

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
        Timeout = TimeSpan.FromSeconds(3)
    };

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
