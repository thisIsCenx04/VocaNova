using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Routing;
using VocaNova.API.Features.AiGrading;
using VocaNova.API.Features.AiGrading.Repositories;
using VocaNova.API.Features.AiGrading.Services;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Features.Auth.Services;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Features.Knn;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Features.Knn.Services;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Features.Lists.Services;
using VocaNova.API.Features.Progress.Repositories;
using VocaNova.API.Features.Progress.Services;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Features.SuperAdmin.Services;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Otp;
using VocaNova.API.Infrastructure.RateLimiting;
using VocaNova.API.Infrastructure.Sms;
using VocaNova.API.Infrastructure.Storage;
using VocaNova.API.Middleware;

EnvironmentFile.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminStatsRepository, AdminStatsRepository>();
builder.Services.AddScoped<IAdminStatsService, AdminStatsService>();
builder.Services.AddScoped<ISuperAdminAccountService, SuperAdminAccountService>();
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<VocaNova.API.Features.Notifications.Repositories.INotificationRepository, VocaNova.API.Features.Notifications.Repositories.NotificationRepository>();
builder.Services.AddScoped<VocaNova.API.Features.Notifications.Services.INotificationService, VocaNova.API.Features.Notifications.Services.NotificationService>();
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IUserListRepository, UserListRepository>();
builder.Services.AddScoped<IUserListService, UserListService>();
builder.Services.AddScoped<IQuizWordPoolRepository, QuizWordPoolRepository>();
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
builder.Services.AddScoped<IQuizSubmitRepository, QuizSubmitRepository>();
builder.Services.AddScoped<IQuizSubmitService, QuizSubmitService>();
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
builder.Services.Configure<AiGradingSettings>(builder.Configuration.GetSection(AiGradingSettings.SectionName));
builder.Services.Configure<KnnOptions>(builder.Configuration.GetSection(KnnOptions.SectionName));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(CloudinarySettings.SectionName));
builder.Services.AddSingleton<IUserProfileCache, RedisUserProfileCache>();
builder.Services.AddSingleton<IWordSearchCache, RedisWordSearchCache>();
builder.Services.AddSingleton<IWordDetailCache, RedisWordDetailCache>();
builder.Services.AddSingleton<ITopicCache, RedisTopicCache>();
builder.Services.AddSingleton<IUserListCache, RedisUserListCache>();
builder.Services.AddSingleton<IProgressSummaryCache, RedisProgressSummaryCache>();
builder.Services.AddSingleton<IKnnTopicRecommendationCache, RedisKnnTopicRecommendationCache>();
builder.Services.AddSingleton<IKnnWordRecommendationCache, RedisKnnWordRecommendationCache>();
builder.Services.AddSingleton<IKnnRebuildStateCache, RedisKnnRebuildStateCache>();
builder.Services.AddSingleton<IKnnRebuildService, KnnRebuildService>();
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection(RateLimitSettings.SectionName));
builder.Services.AddSingleton<IAuthRateLimiter, InMemoryAuthRateLimiter>();
builder.Services.AddSingleton<IAdminKnnTriggerRateLimiter, InMemoryAdminKnnTriggerRateLimiter>();
builder.Services.AddSingleton<IAudioStorage, CloudinaryAudioStorage>();
builder.Services.AddSingleton<IImageStorage, CloudinaryImageStorage>();
builder.Services.AddSingleton<IOtpCodeGenerator, RandomOtpCodeGenerator>();
builder.Services.AddSingleton<ISmsProvider, ConsoleSmsProvider>();
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>((serviceProvider, client) =>
{
    var settings = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiGradingSettings>>()
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
