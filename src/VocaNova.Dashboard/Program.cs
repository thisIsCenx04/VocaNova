using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VocaNova.Dashboard.Services.Api;
using VocaNova.Dashboard.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<VocaNova.Dashboard.Services.Localization.LocalizedModelStateFilter>());

// Đa ngôn ngữ UI dashboard (Tiếng Việt / English) đọc từ cookie, render server-side.
builder.Services.AddScoped<VocaNova.Dashboard.Services.Localization.ITranslator,
    VocaNova.Dashboard.Services.Localization.Translator>();

// Đăng ký route constraint {id:uint} (dùng trong nhiều route dashboard).
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
    options.ConstraintMap["uint"] = typeof(VocaNova.Dashboard.Routing.UIntRouteConstraint));

// Cho phép AJAX (fetch) gửi antiforgery token qua header (F058 sense/audio inline edit).
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// Cấu hình endpoint API backend (F055: dashboard là BFF, mọi data đi qua VocaNova.API).
builder.Services.Configure<DashboardApiOptions>(
    builder.Configuration.GetSection(DashboardApiOptions.SectionName));

// AuthService (shared service layer của dashboard) gọi VocaNova.API qua HttpClient.
builder.Services.AddHttpClient<IDashboardAuthService, DashboardAuthService>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<DashboardApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
});

// API client có Bearer token + auto-refresh khi 401 (F055.1) cho mọi lệnh gọi data admin.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<IVocaNovaApiClient, VocaNovaApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<DashboardApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
}).AddHttpMessageHandler<BearerTokenHandler>();

// Cookie-based auth: token backend được giữ server-side trong cookie, không lộ ra trình duyệt.
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

// Mặc định mọi trang yêu cầu đăng nhập; các action public dùng [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
