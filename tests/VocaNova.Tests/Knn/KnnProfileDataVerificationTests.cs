using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.Contracts.Requests;
using VocaNova.API.Features.Knn.Contracts.Responses;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.DAL.Repositories;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Knn;

public class KnnProfileDataVerificationTests
{
    [Fact]
    public void EfModel_Should_Map_Knn_Source_Tables_And_Columns()
    {
        using var dbContext = CreateDbContext();

        AssertTable<AgeRange>(dbContext, "age_ranges");
        AssertTable<Region>(dbContext, "regions");
        AssertTable<Occupation>(dbContext, "occupations");
        AssertTable<EducationLevel>(dbContext, "education_levels");
        AssertTable<LearningPurpose>(dbContext, "learning_purposes");
        AssertTable<UserLearningProfile>(dbContext, "user_learning_profiles");
        AssertTable<UserTopicPreference>(dbContext, "user_topic_preferences");
        AssertTable<TestAnswer>(dbContext, "test_answers");
        AssertTable<EntityWordTopic>(dbContext, "word_topics");
        AssertTable<EntityUserWordProgress>(dbContext, "user_word_progress");
        AssertTable<UserListWord>(dbContext, "user_list_words");

        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.AgeRangeId), "age_range_id");
        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.RegionId), "region_id");
        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.OccupationId), "occupation_id");
        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.EducationLevelId), "education_level_id");
        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.LearningPurposeId), "learning_purpose_id");
        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.CreatedAt), "created_at");
        AssertColumn<UserLearningProfile>(dbContext, "user_learning_profiles", nameof(UserLearningProfile.UpdatedAt), "updated_at");

        AssertColumn<UserTopicPreference>(dbContext, "user_topic_preferences", nameof(UserTopicPreference.UserId), "user_id");
        AssertColumn<UserTopicPreference>(dbContext, "user_topic_preferences", nameof(UserTopicPreference.TopicId), "topic_id");
        AssertColumn<UserTopicPreference>(dbContext, "user_topic_preferences", nameof(UserTopicPreference.Source), "source");
        AssertColumn<UserTopicPreference>(dbContext, "user_topic_preferences", nameof(UserTopicPreference.Status), "status");
        AssertColumn<UserTopicPreference>(dbContext, "user_topic_preferences", nameof(UserTopicPreference.CreatedAt), "created_at");

        AssertColumn<TestAnswer>(dbContext, "test_answers", nameof(TestAnswer.SessionId), "session_id");
        AssertColumn<TestAnswer>(dbContext, "test_answers", nameof(TestAnswer.WordId), "word_id");
        AssertColumn<TestAnswer>(dbContext, "test_answers", nameof(TestAnswer.IsCorrect), "is_correct");
        AssertColumn<EntityWordTopic>(dbContext, "word_topics", nameof(EntityWordTopic.WordId), "word_id");
        AssertColumn<EntityWordTopic>(dbContext, "word_topics", nameof(EntityWordTopic.TopicId), "topic_id");
        AssertColumn<EntityUserWordProgress>(dbContext, "user_word_progress", nameof(EntityUserWordProgress.UserId), "user_id");
        AssertColumn<EntityUserWordProgress>(dbContext, "user_word_progress", nameof(EntityUserWordProgress.WordId), "word_id");
        AssertColumn<EntityUserWordProgress>(dbContext, "user_word_progress", nameof(EntityUserWordProgress.MasteryLevel), "mastery_level");
        AssertColumn<EntityUserWordProgress>(dbContext, "user_word_progress", nameof(EntityUserWordProgress.SrsInterval), "srs_interval");
        AssertColumn<EntityUserWordProgress>(dbContext, "user_word_progress", nameof(EntityUserWordProgress.EaseFactor), "ease_factor");
        AssertColumn<UserListWord>(dbContext, "user_list_words", nameof(UserListWord.UserId), "user_id");
        AssertColumn<UserListWord>(dbContext, "user_list_words", nameof(UserListWord.ListId), "list_id");
        AssertColumn<UserListWord>(dbContext, "user_list_words", nameof(UserListWord.WordId), "word_id");
        AssertColumn<UserListWord>(dbContext, "user_list_words", nameof(UserListWord.Status), "status");

        dbContext.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Should()
            .NotContain(new[] { "recommendations", "knn_model_configs" });
    }

    [Fact]
    public void KnnOptions_Should_Bind_From_Configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Knn:Onboarding:KValue"] = "7",
                ["Knn:Onboarding:DefaultTopicLimit"] = "12",
                ["Knn:Onboarding:MinSimilarity"] = "0.25",
                ["Knn:Onboarding:CacheTtlMinutes"] = "45",
                ["Knn:Learning:KValue"] = "9",
                ["Knn:Learning:MinSessions"] = "4",
                ["Knn:Learning:MinSimilarity"] = "0.2",
                ["Knn:Learning:RecommendationCount"] = "80",
                ["Knn:Learning:RebuildIntervalHours"] = "18",
                ["Knn:Learning:CacheTtlMinutes"] = "90",
            })
            .Build();

        using var serviceProvider = new ServiceCollection()
            .Configure<KnnOptions>(configuration.GetSection(KnnOptions.SectionName))
            .BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<KnnOptions>>().Value;

        options.Onboarding.KValue.Should().Be(7);
        options.Onboarding.DefaultTopicLimit.Should().Be(12);
        options.Onboarding.MinSimilarity.Should().Be(0.25);
        options.Onboarding.CacheTtlMinutes.Should().Be(45);
        options.Learning.KValue.Should().Be(9);
        options.Learning.MinSessions.Should().Be(4);
        options.Learning.MinSimilarity.Should().Be(0.2);
        options.Learning.RecommendationCount.Should().Be(80);
        options.Learning.RebuildIntervalHours.Should().Be(18);
        options.Learning.CacheTtlMinutes.Should().Be(90);
    }

    [Fact]
    public async Task KnnProfileRepository_Should_Read_Profile_And_Active_Topic_Preferences()
    {
        await using var dbContext = CreateDbContext();
        await SeedKnnProfileDataAsync(dbContext);
        var repository = new KnnProfileRepository(dbContext);

        var profile = await repository.GetLearningProfileAsync(1);
        var preferences = await repository.GetActiveTopicPreferencesAsync(1);

        profile.Should().NotBeNull();
        profile!.AgeRangeName.Should().Be("18-24");
        profile.RegionName.Should().Be("Ho Chi Minh");
        profile.OccupationName.Should().Be("Student");
        profile.EducationLevelName.Should().Be("University");
        profile.LearningPurposeName.Should().Be("IELTS");

        preferences.Should().HaveCount(2);
        preferences.Select(preference => preference.TopicName)
            .Should()
            .Equal("Business", "Travel");
        preferences.Select(preference => preference.Source)
            .Should()
            .Contain(new[] { "knn_suggested", "user_selected" });
    }

    [Fact]
    public void WordRecommendationItem_Should_Expose_Redis_Snapshot_Shape()
    {
        var item = new WordRecommendationItem(
            10,
            "achieve",
            "/uh-cheev/",
            "dat duoc",
            "https://example.com/image.png",
            "B1",
            0.87);

        item.WordId.Should().Be(10);
        item.Word.Should().Be("achieve");
        item.PhoneticUk.Should().Be("/uh-cheev/");
        item.PrimaryMeaning.Should().Be("dat duoc");
        item.ImageUrl.Should().Be("https://example.com/image.png");
        item.CefrLevel.Should().Be("B1");
        item.Score.Should().Be(0.87);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static void AssertTable<TEntity>(VocaNovaDbContext dbContext, string expectedTable)
    {
        dbContext.Model.FindEntityType(typeof(TEntity))!
            .GetTableName()
            .Should()
            .Be(expectedTable);
    }

    private static void AssertColumn<TEntity>(
        VocaNovaDbContext dbContext,
        string tableName,
        string propertyName,
        string expectedColumn)
    {
        var storeObject = StoreObjectIdentifier.Table(tableName, null);
        GetProperty<TEntity>(dbContext, propertyName)
            .GetColumnName(storeObject)
            .Should()
            .Be(expectedColumn);
    }

    private static IProperty GetProperty<TEntity>(VocaNovaDbContext dbContext, string propertyName)
    {
        return dbContext.Model.FindEntityType(typeof(TEntity))!
            .FindProperty(propertyName)!;
    }

    private static async Task SeedKnnProfileDataAsync(VocaNovaDbContext dbContext)
    {
        var now = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var ageRange = new AgeRange
        {
            AgeRangeId = 1,
            Name = "18-24",
            DisplayOrder = 1,
            Status = UserStatus.Active,
        };
        var region = new Region
        {
            RegionId = 1,
            Name = "Ho Chi Minh",
            Code = "HCM",
            Status = UserStatus.Active,
        };
        var occupation = new Occupation
        {
            OccupationId = 1,
            Name = "Student",
            Status = UserStatus.Active,
        };
        var educationLevel = new EducationLevel
        {
            EducationLevelId = 1,
            Name = "University",
            DisplayOrder = 1,
            Status = UserStatus.Active,
        };
        var learningPurpose = new LearningPurpose
        {
            LearningPurposeId = 1,
            Name = "IELTS",
            Status = UserStatus.Active,
        };
        var travel = new Topic
        {
            TopicId = 1,
            TopicName = "Travel",
            TopicNameVi = "Du lich",
            Icon = "plane",
            Status = UserStatus.Active,
        };
        var business = new Topic
        {
            TopicId = 2,
            TopicName = "Business",
            TopicNameVi = "Kinh doanh",
            Icon = "briefcase",
            Status = UserStatus.Active,
        };
        var inactive = new Topic
        {
            TopicId = 3,
            TopicName = "Inactive",
            Status = UserStatus.Active,
        };

        dbContext.AddRange(ageRange, region, occupation, educationLevel, learningPurpose, travel, business, inactive);
        dbContext.UserLearningProfiles.Add(new UserLearningProfile
        {
            UserId = 1,
            AgeRangeId = ageRange.AgeRangeId,
            AgeRange = ageRange,
            RegionId = region.RegionId,
            Region = region,
            OccupationId = occupation.OccupationId,
            Occupation = occupation,
            EducationLevelId = educationLevel.EducationLevelId,
            EducationLevel = educationLevel,
            LearningPurposeId = learningPurpose.LearningPurposeId,
            LearningPurpose = learningPurpose,
            CreatedAt = now,
            UpdatedAt = now,
        });
        dbContext.UserTopicPreferences.AddRange(
            new UserTopicPreference
            {
                UserId = 1,
                TopicId = travel.TopicId,
                Topic = travel,
                Source = "user_selected",
                Status = UserStatus.Active,
                CreatedAt = now,
            },
            new UserTopicPreference
            {
                UserId = 1,
                TopicId = business.TopicId,
                Topic = business,
                Source = "knn_suggested",
                Status = UserStatus.Active,
                CreatedAt = now.AddMinutes(1),
            },
            new UserTopicPreference
            {
                UserId = 1,
                TopicId = inactive.TopicId,
                Topic = inactive,
                Source = "onboarding",
                Status = UserStatus.Deleted,
                CreatedAt = now.AddMinutes(2),
            });

        await dbContext.SaveChangesAsync();
    }
}
