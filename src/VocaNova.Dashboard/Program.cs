using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VocaNova.Dashboard.Services.Api;
using VocaNova.Dashboard.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// R2: localization EN + VI.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.Configure<DashboardApiOptions>(
    builder.Configuration.GetSection(DashboardApiOptions.SectionName));
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VocaNova.Dashboard.Auth";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddHttpClient<IDashboardAuthService, DashboardAuthService>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<DashboardApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
});

// API client có Bearer token + auto-refresh (F055.1).
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<IVocaNovaApiClient, VocaNovaApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<DashboardApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
}).AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// R2: áp culture từ cookie (CookieRequestCultureProvider mặc định), hỗ trợ EN + VI.
var supportedCultures = new[] { "en", "vi" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

partial class Program;
