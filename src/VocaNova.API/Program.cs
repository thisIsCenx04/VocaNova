using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Abstractions.Configuration;
using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Services;
using VocaNova.API.Features.Auth.DAL.Repositories;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Notifications.BLL.Abstractions;
using VocaNova.API.Features.Notifications.BLL.Services;
using VocaNova.API.Features.Progress.BLL.Services;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Features.Lists.BLL.Services;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Routing;
using VocaNova.API.Infrastructure.Caching.Progress;
using VocaNova.API.Infrastructure.Caching.Lists;
using VocaNova.API.Features.Notifications.DAL.Repositories;
using VocaNova.API.Features.Progress.DAL.Repositories;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Notifications.BLL.Abstractions;
using VocaNova.API.Features.Notifications.BLL.Services;
using VocaNova.API.Features.Progress.BLL.Services;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Features.Lists.BLL.Services;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Routing;
using VocaNova.API.Infrastructure.Caching.Progress;
using VocaNova.API.Infrastructure.Caching.Lists;
using VocaNova.API.Features.Notifications.DAL.Repositories;
using VocaNova.API.Features.Progress.DAL.Repositories;
using VocaNova.API.Features.Dictionary.DAL.Repositories;
using VocaNova.API.Features.Lists.DAL.Repositories;
using VocaNova.API.Features.AiGrading.BLL.Abstractions;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Features.AiGrading.DAL.Repositories;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Services;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Services;
using VocaNova.API.Features.Knn.DAL.Repositories;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Services;
using VocaNova.API.Features.Quiz.DAL.Repositories;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Services;
using VocaNova.API.Features.SuperAdmin.DAL.Repositories;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Caching.Auth;
using VocaNova.API.Infrastructure.Caching.Knn;
using VocaNova.API.Infrastructure.Caching.Quiz;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Infrastructure.ExternalServices.Gemini;
using VocaNova.API.Infrastructure.HostedServices;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Transactions;
using VocaNova.API.Infrastructure.Otp;
using VocaNova.API.Infrastructure.RateLimiting;
using VocaNova.API.Infrastructure.Sms;
using VocaNova.API.Infrastructure.Storage;
using VocaNova.API.Middleware;
using DictionaryTopicCache = VocaNova.API.Features.Dictionary.BLL.Abstractions.ITopicCache;
using DictionaryWordDetailCache = VocaNova.API.Features.Dictionary.BLL.Abstractions.IWordDetailCache;
using DictionaryWordSearchCache = VocaNova.API.Features.Dictionary.BLL.Abstractions.IWordSearchCache;
using DictionaryRedisTopicCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisTopicCache;
using DictionaryRedisWordDetailCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisWordDetailCache;
using DictionaryRedisWordSearchCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisWordSearchCache;

// Publishes .env as process environment variables — DatabaseConnection reads its connection
// string straight from the environment rather than from IConfiguration.
EnvironmentFile.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

// The environment-variable provider above is a one-time snapshot with no file watcher. Layering
// the same file in as a watched source is what lets the admin settings screens change .env and
// have the running app pick it up.
var envFilePath = EnvironmentFile.FindPath();
if (envFilePath is not null)
{
    builder.Configuration.AddEnvFile(envFilePath);
}

builder.Services.AddDbContext<VocaNovaDbContext>(options =>
{
    var connectionString = DatabaseConnection.GetConnectionString();

    options.UseMySql(
        connectionString,
        DatabaseConnection.GetServerVersion());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap["uint"] = typeof(UIntRouteConstraint);
});
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddVocaNovaAuthorization();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthAccountRepository, AuthAccountRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IApplicationTransactionManager, EfApplicationTransactionManager>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminStatsRepository, AdminStatsRepository>();
builder.Services.AddScoped<IAdminStatsService, AdminStatsService>();
builder.Services.AddScoped<ISuperAdminAccountRepository, SuperAdminAccountRepository>();
builder.Services.AddScoped<ISuperAdminAccountService, SuperAdminAccountService>();
builder.Services.AddScoped<IRoleManagementRepository, RoleManagementRepository>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IWordAdminRepository, WordAdminRepository>();
builder.Services.AddScoped<IWordAdminService, WordAdminService>();
builder.Services.AddScoped<IWordReadRepository, WordReadRepository>();
builder.Services.AddScoped<IWordReadService, WordReadService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITopicAdminRepository, TopicAdminRepository>();
builder.Services.AddScoped<ITopicAdminService, TopicAdminService>();
builder.Services.AddScoped<ITopicReadRepository, TopicReadRepository>();
builder.Services.AddScoped<ITopicReadService, TopicReadService>();
builder.Services.AddScoped<IListQueryRepository, ListQueryRepository>();
builder.Services.AddScoped<IListQueryService, ListQueryService>();
builder.Services.AddScoped<IPersonalTopicQueryRepository, PersonalTopicQueryRepository>();
builder.Services.AddScoped<IPersonalTopicQueryService, PersonalTopicQueryService>();
builder.Services.AddScoped<IListMutationRepository, ListMutationRepository>();
builder.Services.AddScoped<IListMutationService, ListMutationService>();
builder.Services.AddScoped<IPersonalTopicMutationRepository, PersonalTopicMutationRepository>();
builder.Services.AddScoped<IPersonalTopicMutationService, PersonalTopicMutationService>();
builder.Services.AddScoped<IQuizPoolRepository, QuizPoolRepository>();
builder.Services.AddScoped<IQuizSessionBuilder, QuizSessionBuilder>();
builder.Services.AddScoped<IQuizQuestionRepository, QuizQuestionRepository>();
builder.Services.AddScoped<IQuizQuestionBuilder, QuizQuestionBuilder>();
builder.Services.AddScoped<IQuizSessionRepository, QuizSessionRepository>();
builder.Services.AddScoped<IQuizSessionService, QuizSessionService>();
builder.Services.AddScoped<IAnswerGrader, ExactTypingGrader>();
builder.Services.AddScoped<IAnswerGrader, MultipleChoiceGrader>();
builder.Services.AddScoped<ISrsRepository, SrsRepository>();
builder.Services.AddScoped<ISrsService, SrsService>();
builder.Services.AddScoped<IAiGradingCacheRepository, AiGradingCacheRepository>();
builder.Services.AddScoped<IAiGradingProvider, GeminiAiGradingProvider>();
builder.Services.AddScoped<IAiGradingService, CachedAiGradingService>();
builder.Services.AddScoped<IQuizSubmissionRepository, QuizSubmissionRepository>();
builder.Services.AddScoped<IQuizSubmissionService, QuizSubmissionService>();
builder.Services.AddScoped<IQuizResultRepository, QuizResultRepository>();
builder.Services.AddScoped<IQuizResultService, QuizResultService>();
builder.Services.AddScoped<IQuizHistoryRepository, QuizHistoryRepository>();
builder.Services.AddScoped<IQuizHistoryService, QuizHistoryService>();
builder.Services.AddScoped<IProgressSummaryRepository, ProgressSummaryRepository>();
builder.Services.AddScoped<IProgressSummaryService, ProgressSummaryService>();
builder.Services.AddScoped<IProgressAnalyticsRepository, ProgressAnalyticsRepository>();
builder.Services.AddScoped<IProgressAnalyticsService, ProgressAnalyticsService>();
builder.Services.AddScoped<IKnnProfileRepository, KnnProfileRepository>();
builder.Services.AddScoped<IKnnOnboardingService, KnnOnboardingService>();
builder.Services.AddScoped<IKnnLearningRepository, KnnLearningRepository>();
builder.Services.AddScoped<IKnnLearningService, KnnLearningService>();
builder.Services.AddScoped<IAdminKnnLookupRepository, AdminKnnLookupRepository>();
builder.Services.AddScoped<IAdminKnnLookupService, AdminKnnLookupService>();
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(RedisSettings.SectionName));
builder.Services.Configure<AiGradingConfiguration>(builder.Configuration.GetSection(AiGradingConfiguration.SectionName));
builder.Services.Configure<KnnOptions>(builder.Configuration.GetSection(KnnOptions.SectionName));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(CloudinarySettings.SectionName));
builder.Services.Configure<AuthTokenOptions>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<IUserProfileCache, RedisUserProfileCache>();
builder.Services.AddSingleton<DictionaryWordSearchCache, DictionaryRedisWordSearchCache>();
builder.Services.AddSingleton<DictionaryWordDetailCache, DictionaryRedisWordDetailCache>();
builder.Services.AddSingleton<DictionaryTopicCache, DictionaryRedisTopicCache>();
builder.Services.AddSingleton<IUserListCache, RedisUserListCache>();
builder.Services.AddSingleton<IProgressSummaryCache, RedisProgressSummaryCache>();
builder.Services.AddSingleton<IQuizPoolCache, RedisQuizPoolCache>();
builder.Services.AddSingleton<IKnnTopicRecommendationCache, RedisKnnTopicRecommendationCache>();
builder.Services.AddSingleton<IKnnWordRecommendationCache, RedisKnnWordRecommendationCache>();
builder.Services.AddSingleton<IKnnRebuildStateCache, RedisKnnRebuildStateCache>();
builder.Services.AddSingleton<IKnnRebuildService, KnnRebuildService>();
builder.Services.AddSingleton<IRuntimeSettingsStore, RedisRuntimeSettingsStore>();
builder.Services.AddSingleton<IRuntimeConfigWriter, EnvFileRuntimeConfigWriter>();
builder.Services.AddSingleton<IKnnRuntimeConfigurationService, KnnRuntimeConfigurationService>();
builder.Services.AddSingleton<IAiGradingConfigurationService, AiGradingConfigurationService>();
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection(RateLimitSettings.SectionName));
builder.Services.Configure<AuthRateLimitOptions>(builder.Configuration.GetSection(RateLimitSettings.SectionName));
builder.Services.AddSingleton<IAuthRateLimiter, InMemoryAuthRateLimiter>();
builder.Services.AddSingleton<IAdminKnnTriggerRateLimiter, InMemoryAdminKnnTriggerRateLimiter>();
builder.Services.AddSingleton<IWordAudioStorage, CloudinaryWordAudioStorage>();
builder.Services.AddSingleton<IWordImageStorage, CloudinaryWordImageStorage>();
builder.Services.AddSingleton<IAvatarStorage, CloudinaryAvatarStorage>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
builder.Services.AddSingleton<IOtpCodeGenerator, RandomOtpCodeGenerator>();
builder.Services
    .AddOptions<SpeedSmsSettings>()
    .Bind(builder.Configuration.GetSection(SpeedSmsSettings.SectionName))
    .Validate(
        settings => !settings.Enabled
            || (!string.IsNullOrWhiteSpace(settings.AccessToken)
                && !string.IsNullOrWhiteSpace(settings.DeviceId)
                && settings.SmsType == 5
                && Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps),
        "Enabled SpeedSMS requires an access token, Android Device ID, sms_type 5, and an HTTPS BaseUrl.")
    .ValidateOnStart();
if (builder.Configuration.GetValue<bool>("SpeedSms:Enabled"))
{
    builder.Services.AddHttpClient<ISmsSender, SpeedSmsProvider>((serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<SpeedSmsSettings>>()
            .Value;
        client.BaseAddress = new Uri($"{settings.BaseUrl.TrimEnd('/')}/");
        client.Timeout = TimeSpan.FromSeconds(15);
    });
}
else
{
    builder.Services.AddSingleton<ISmsSender, ConsoleSmsProvider>();
}
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>((serviceProvider, client) =>
{
    var settings = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiGradingConfiguration>>()
        .Value;
    var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
        ? "https://generativelanguage.googleapis.com/v1beta"
        : settings.Endpoint.TrimEnd('/');

    client.BaseAddress = new Uri($"{endpoint}/");
});
builder.Services.AddSingleton<IAuditLogQueue, AuditLogQueue>();
builder.Services.AddHostedService<AuditLogBackgroundService>();
builder.Services.AddHostedService<KnnWordRecommendationJob>();
builder.Services.AddSwaggerWithJwtBearer();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AuditLogMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(ApiResponseFormatter.Success(new { status = "ok", service = "VocaNova.API" })))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

public partial class Program;
