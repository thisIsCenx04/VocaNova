using Microsoft.EntityFrameworkCore;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence;

public partial class VocaNovaDbContext : DbContext
{
    public VocaNovaDbContext(DbContextOptions<VocaNovaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AgeRange> AgeRanges { get; set; }

    public virtual DbSet<AiGradingCache> AiGradingCaches { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<EducationLevel> EducationLevels { get; set; }

    public virtual DbSet<LearningPurpose> LearningPurposes { get; set; }

    public virtual DbSet<Occupation> Occupations { get; set; }

    public virtual DbSet<OtpVerification> OtpVerifications { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Region> Regions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TestAnswer> TestAnswers { get; set; }

    public virtual DbSet<TestSession> TestSessions { get; set; }

    public virtual DbSet<TestSessionTopic> TestSessionTopics { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuth> UserAuths { get; set; }

    public virtual DbSet<UserLearningProfile> UserLearningProfiles { get; set; }

    public virtual DbSet<UserList> UserLists { get; set; }

    public virtual DbSet<UserListWord> UserListWords { get; set; }

    public virtual DbSet<UserListWordStat> UserListWordStats { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserTopicPreference> UserTopicPreferences { get; set; }

    public virtual DbSet<UserWordProgress> UserWordProgresses { get; set; }

    public virtual DbSet<Word> Words { get; set; }

    public virtual DbSet<WordAudioAsset> WordAudioAssets { get; set; }

    public virtual DbSet<WordDerivedForm> WordDerivedForms { get; set; }

    public virtual DbSet<WordExample> WordExamples { get; set; }

    public virtual DbSet<WordIdiom> WordIdioms { get; set; }

    public virtual DbSet<WordRelation> WordRelations { get; set; }

    public virtual DbSet<WordSense> WordSenses { get; set; }

    public virtual DbSet<WordTopic> WordTopics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VocaNovaDbContext).Assembly);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

