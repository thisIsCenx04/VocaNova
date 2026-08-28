using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Abstractions.Configuration;
using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Services;
using VocaNova.API.Features.Admin.DAL.Repositories;
using VocaNova.API.Features.AiGrading.BLL.Abstractions;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Features.AiGrading.DAL.Repositories;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Services;
using VocaNova.API.Features.Auth.DAL.Repositories;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Features.Dictionary.BLL.Services.IServices;
using VocaNova.API.Features.Dictionary.DAL.Repositories;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Services;
using VocaNova.API.Features.Knn.DAL.Repositories;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Services;
using VocaNova.API.Features.Lists.DAL.Repositories;
using VocaNova.API.Features.Notifications.BLL.Abstractions;
using VocaNova.API.Features.Notifications.BLL.Services;
using VocaNova.API.Features.Notifications.DAL.Repositories;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Services;
using VocaNova.API.Features.Progress.DAL.Repositories;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Services;
using VocaNova.API.Features.Quiz.BLL.Services.IServices;
using VocaNova.API.Features.Quiz.DAL.Repositories;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Services;
using VocaNova.API.Features.SuperAdmin.DAL.Repositories;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Caching.Auth;
using VocaNova.API.Infrastructure.Caching.Knn;
using VocaNova.API.Infrastructure.Caching.Lists;
using VocaNova.API.Infrastructure.Caching.Progress;
using VocaNova.API.Infrastructure.Caching.Quiz;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Infrastructure.ExternalServices.Gemini;
using VocaNova.API.Infrastructure.HostedServices;
using VocaNova.API.Infrastructure.Otp;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Transactions;
using VocaNova.API.Infrastructure.RateLimiting;
using VocaNova.API.Infrastructure.Sms;
using VocaNova.API.Infrastructure.Storage;
using DictionaryRedisTopicCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisTopicCache;
using DictionaryRedisWordDetailCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisWordDetailCache;
using DictionaryRedisWordSearchCache = VocaNova.API.Infrastructure.Caching.Dictionary.RedisWordSearchCache;
using DictionaryTopicCache = VocaNova.API.Features.Dictionary.BLL.Abstractions.ITopicCache;
using DictionaryWordDetailCache = VocaNova.API.Features.Dictionary.BLL.Abstractions.IWordDetailCache;
using DictionaryWordSearchCache = VocaNova.API.Features.Dictionary.BLL.Abstractions.IWordSearchCache;
using VocaNova.API.Features.Notifications.BLL.Services.IServices;
using VocaNova.API.Features.Knn.BLL.Services.IServices;
using VocaNova.API.Features.Lists.BLL.Services.IServices;
using VocaNova.API.Features.Progress.BLL.Services.IServices;
using VocaNova.API.Features.SuperAdmin.BLL.Services.IServices;
using VocaNova.API.Features.Admin.BLL.Services.IServices;
using VocaNova.API.Features.Auth.BLL.Services.IServices;
using VocaNova.API.Features.AiGrading.BLL.Services.IServices;

namespace VocaNova.API.DependencyInjection;

public static class VocaNovaServiceCollectionExtensions
{
    public static IServiceCollection AddBLL(this IServiceCollection services)
    {
        return services
            .AddAuthBLL()
            .AddAdminBLL()
            .AddSuperAdminBLL()
            .AddDictionaryBLL()
            .AddNotificationsBLL()
            .AddListsBLL()
            .AddQuizBLL()
            .AddAiGradingBLL()
            .AddProgressBLL()
            .AddKnnBLL();
    }

    public static IServiceCollection AddDAL(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddPersistence()
            .AddSharedInfrastructure(configuration)
            .AddAuthDAL(configuration)
            .AddAdminDAL()
            .AddSuperAdminDAL()
            .AddDictionaryDAL()
            .AddNotificationsDAL()
            .AddListsDAL()
            .AddQuizDAL()
            .AddAiGradingDAL(configuration)
            .AddProgressDAL()
            .AddKnnDAL()
            .AddHostedInfrastructure();
    }

    private static IServiceCollection AddAuthBLL(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    private static IServiceCollection AddAdminBLL(this IServiceCollection services)
    {
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminStatsService, AdminStatsService>();

        return services;
    }

    private static IServiceCollection AddSuperAdminBLL(this IServiceCollection services)
    {
        services.AddScoped<ISuperAdminAccountService, SuperAdminAccountService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();

        return services;
    }

    private static IServiceCollection AddDictionaryBLL(this IServiceCollection services)
    {
        services.AddScoped<IWordAdminService, WordAdminService>();
        services.AddScoped<IWordReadService, WordReadService>();
        services.AddScoped<ITopicAdminService, TopicAdminService>();
        services.AddScoped<ITopicReadService, TopicReadService>();

        return services;
    }

    private static IServiceCollection AddNotificationsBLL(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    private static IServiceCollection AddListsBLL(this IServiceCollection services)
    {
        services.AddScoped<IListQueryService, ListQueryService>();
        services.AddScoped<IPersonalTopicQueryService, PersonalTopicQueryService>();
        services.AddScoped<IListMutationService, ListMutationService>();
        services.AddScoped<IPersonalTopicMutationService, PersonalTopicMutationService>();

        return services;
    }

    private static IServiceCollection AddQuizBLL(this IServiceCollection services)
    {
        services.AddScoped<IQuizSessionBuilder, QuizSessionBuilder>();
        services.AddScoped<IQuizQuestionBuilder, QuizQuestionBuilder>();
        services.AddScoped<IQuizSessionService, QuizSessionService>();
        services.AddScoped<IAnswerGrader, ExactTypingGrader>();
        services.AddScoped<IAnswerGrader, MultipleChoiceGrader>();
        services.AddScoped<ISrsService, SrsService>();
        services.AddScoped<IQuizSubmissionService, QuizSubmissionService>();
        services.AddScoped<IQuizResultService, QuizResultService>();
        services.AddScoped<IQuizHistoryService, QuizHistoryService>();

        return services;
    }

    private static IServiceCollection AddAiGradingBLL(this IServiceCollection services)
    {
        services.AddScoped<IAiGradingService, CachedAiGradingService>();
        services.AddSingleton<IAiGradingConfigurationService, AiGradingConfigurationService>();

        return services;
    }

    private static IServiceCollection AddProgressBLL(this IServiceCollection services)
    {
        services.AddScoped<IProgressSummaryService, ProgressSummaryService>();
        services.AddScoped<IProgressAnalyticsService, ProgressAnalyticsService>();

        return services;
    }

    private static IServiceCollection AddKnnBLL(this IServiceCollection services)
    {
        services.AddScoped<IKnnOnboardingService, KnnOnboardingService>();
        services.AddScoped<IKnnLearningService, KnnLearningService>();
        services.AddScoped<IAdminKnnLookupService, AdminKnnLookupService>();
        services.AddSingleton<IKnnRebuildService, KnnRebuildService>();
        services.AddSingleton<IKnnRuntimeConfigurationService, KnnRuntimeConfigurationService>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddDbContext<VocaNovaDbContext>(options =>
        {
            var connectionString = DatabaseConnection.GetConnectionString();

            options.UseMySql(
                connectionString,
                DatabaseConnection.GetServerVersion());
        });

        services.AddScoped<IApplicationTransactionManager, EfApplicationTransactionManager>();

        return services;
    }

    private static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        services.Configure<AiGradingConfiguration>(configuration.GetSection(AiGradingConfiguration.SectionName));
        services.Configure<KnnOptions>(configuration.GetSection(KnnOptions.SectionName));
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.Configure<AuthTokenOptions>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.SectionName));
        services.Configure<AuthRateLimitOptions>(configuration.GetSection(RateLimitSettings.SectionName));

        services.AddSingleton<IRuntimeSettingsStore, RedisRuntimeSettingsStore>();
        services.AddSingleton<IRuntimeConfigWriter, EnvFileRuntimeConfigWriter>();
        services.AddSingleton<IAuditLogQueue, AuditLogQueue>();

        return services;
    }

    private static IServiceCollection AddAuthDAL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAuthAccountRepository, AuthAccountRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();

        services.AddSingleton<IUserProfileCache, RedisUserProfileCache>();
        services.AddSingleton<IAuthRateLimiter, InMemoryAuthRateLimiter>();
        services.AddSingleton<IAvatarStorage, CloudinaryAvatarStorage>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
        services.AddSingleton<IOtpCodeGenerator, RandomOtpCodeGenerator>();

        services
            .AddOptions<SpeedSmsSettings>()
            .Bind(configuration.GetSection(SpeedSmsSettings.SectionName))
            .Validate(
                settings => !settings.Enabled
                    || (!string.IsNullOrWhiteSpace(settings.AccessToken)
                        && !string.IsNullOrWhiteSpace(settings.DeviceId)
                        && settings.SmsType == 5
                        && Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri)
                        && uri.Scheme == Uri.UriSchemeHttps),
                "Enabled SpeedSMS requires an access token, Android Device ID, sms_type 5, and an HTTPS BaseUrl.")
            .ValidateOnStart();

        if (configuration.GetValue<bool>("SpeedSms:Enabled"))
        {
            services.AddHttpClient<ISmsSender, SpeedSmsProvider>((serviceProvider, client) =>
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
            services.AddSingleton<ISmsSender, ConsoleSmsProvider>();
        }

        return services;
    }

    private static IServiceCollection AddAdminDAL(this IServiceCollection services)
    {
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminStatsRepository, AdminStatsRepository>();

        return services;
    }

    private static IServiceCollection AddSuperAdminDAL(this IServiceCollection services)
    {
        services.AddScoped<ISuperAdminAccountRepository, SuperAdminAccountRepository>();
        services.AddScoped<IRoleManagementRepository, RoleManagementRepository>();

        return services;
    }

    private static IServiceCollection AddDictionaryDAL(this IServiceCollection services)
    {
        services.AddScoped<IWordAdminRepository, WordAdminRepository>();
        services.AddScoped<IWordReadRepository, WordReadRepository>();
        services.AddScoped<ITopicAdminRepository, TopicAdminRepository>();
        services.AddScoped<ITopicReadRepository, TopicReadRepository>();

        services.AddSingleton<DictionaryWordSearchCache, DictionaryRedisWordSearchCache>();
        services.AddSingleton<DictionaryWordDetailCache, DictionaryRedisWordDetailCache>();
        services.AddSingleton<DictionaryTopicCache, DictionaryRedisTopicCache>();
        services.AddSingleton<IWordAudioStorage, CloudinaryWordAudioStorage>();
        services.AddSingleton<IWordImageStorage, CloudinaryWordImageStorage>();

        return services;
    }

    private static IServiceCollection AddNotificationsDAL(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    private static IServiceCollection AddListsDAL(this IServiceCollection services)
    {
        services.AddScoped<IListQueryRepository, ListQueryRepository>();
        services.AddScoped<IPersonalTopicQueryRepository, PersonalTopicQueryRepository>();
        services.AddScoped<IListMutationRepository, ListMutationRepository>();
        services.AddScoped<IPersonalTopicMutationRepository, PersonalTopicMutationRepository>();

        services.AddSingleton<IUserListCache, RedisUserListCache>();

        return services;
    }

    private static IServiceCollection AddQuizDAL(this IServiceCollection services)
    {
        services.AddScoped<IQuizPoolRepository, QuizPoolRepository>();
        services.AddScoped<IQuizQuestionRepository, QuizQuestionRepository>();
        services.AddScoped<IQuizSessionRepository, QuizSessionRepository>();
        services.AddScoped<ISrsRepository, SrsRepository>();
        services.AddScoped<IQuizSubmissionRepository, QuizSubmissionRepository>();
        services.AddScoped<IQuizResultRepository, QuizResultRepository>();
        services.AddScoped<IQuizHistoryRepository, QuizHistoryRepository>();

        services.AddSingleton<IQuizPoolCache, RedisQuizPoolCache>();

        return services;
    }

    private static IServiceCollection AddAiGradingDAL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAiGradingCacheRepository, AiGradingCacheRepository>();
        services.AddScoped<IAiGradingProvider, GeminiAiGradingProvider>();

        services.AddHttpClient<IGeminiClient, GeminiClient>((serviceProvider, client) =>
        {
            var settings = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiGradingConfiguration>>()
                .Value;
            var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
                ? "https://generativelanguage.googleapis.com/v1beta"
                : settings.Endpoint.TrimEnd('/');

            client.BaseAddress = new Uri($"{endpoint}/");
        });

        return services;
    }

    private static IServiceCollection AddProgressDAL(this IServiceCollection services)
    {
        services.AddScoped<IProgressSummaryRepository, ProgressSummaryRepository>();
        services.AddScoped<IProgressAnalyticsRepository, ProgressAnalyticsRepository>();

        services.AddSingleton<IProgressSummaryCache, RedisProgressSummaryCache>();

        return services;
    }

    private static IServiceCollection AddKnnDAL(this IServiceCollection services)
    {
        services.AddScoped<IKnnProfileRepository, KnnProfileRepository>();
        services.AddScoped<IKnnLearningRepository, KnnLearningRepository>();
        services.AddScoped<IAdminKnnLookupRepository, AdminKnnLookupRepository>();

        services.AddSingleton<IKnnTopicRecommendationCache, RedisKnnTopicRecommendationCache>();
        services.AddSingleton<IKnnWordRecommendationCache, RedisKnnWordRecommendationCache>();
        services.AddSingleton<IKnnRebuildStateCache, RedisKnnRebuildStateCache>();
        services.AddSingleton<IAdminKnnTriggerRateLimiter, InMemoryAdminKnnTriggerRateLimiter>();

        return services;
    }

    private static IServiceCollection AddHostedInfrastructure(this IServiceCollection services)
    {
        services.AddHostedService<AuditLogBackgroundService>();
        services.AddHostedService<KnnWordRecommendationJob>();

        return services;
    }
}
