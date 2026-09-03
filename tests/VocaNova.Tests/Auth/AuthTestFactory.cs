using Microsoft.Extensions.Options;
using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Common.Constants;

namespace VocaNova.Tests.Auth;

internal static class AuthTestFactory
{
    public static AuthService CreateService(
        VocaNovaDbContext dbContext,
        IGoogleIdentityProvider? googleIdentityProvider = null,
        IOtpCodeGenerator? otpCodeGenerator = null,
        ISmsSender? smsSender = null,
        IUserProfileCache? userProfileCache = null,
        IKnnTopicRecommendationCache? knnTopicRecommendationCache = null,
        IAvatarStorage? avatarStorage = null)
    {
        return new AuthService(
            new AuthAccountRepository(dbContext),
            new RefreshTokenRepository(dbContext),
            new OtpRepository(dbContext),
            new EfApplicationTransactionManager(dbContext),
            CreateJwtTokenService(),
            googleIdentityProvider ?? new StaticGoogleIdentityProvider(null),
            new BcryptPasswordHasher(),
            new Sha256RefreshTokenHasher(),
            Options.Create(CreateAuthTokenOptions()),
            userProfileCache,
            otpCodeGenerator,
            smsSender,
            Options.Create(new AuthRateLimitOptions()),
            knnTopicRecommendationCache,
            avatarStorage);
    }

    public static JwtTokenService CreateJwtTokenService() =>
        new(Options.Create(CreateJwtSettings()));

    public static JwtSettings CreateJwtSettings() =>
        new()
        {
            Issuer = "VocaNova.Tests",
            Audience = "VocaNova.Tests.Clients",
            SecretKey = "THIS_IS_A_TEST_SECRET_KEY_WITH_32_CHARS_MIN",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        };

    private static AuthTokenOptions CreateAuthTokenOptions() =>
        new()
        {
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        };

    public sealed class StaticGoogleIdentityProvider : IGoogleIdentityProvider
    {
        private readonly GoogleIdentity? _identity;

        public StaticGoogleIdentityProvider(GoogleIdentity? identity)
        {
            _identity = identity;
        }

        public Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(_identity);
    }

    public sealed class StaticOtpCodeGenerator : IOtpCodeGenerator
    {
        private readonly string _code;

        public StaticOtpCodeGenerator(string code)
        {
            _code = code;
        }

        public string Generate() => _code;
    }

    public sealed class RecordingSmsSender : ISmsSender
    {
        public List<(string Phone, string Code)> SentMessages { get; } = [];

        public Task SendOtpAsync(string phone, string code, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((phone, code));
            return Task.CompletedTask;
        }
    }
}
