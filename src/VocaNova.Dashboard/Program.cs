using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VocaNova.Dashboard.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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
