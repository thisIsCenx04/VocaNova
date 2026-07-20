using FluentAssertions;
using VocaNova.API.Common.Constants;

namespace VocaNova.Tests.Shared;

public class ConstantsTests
{
    [Fact]
    public void QuestionType_Should_Use_Database_String_Values()
    {
        QuestionType.WordToMeaning.Should().Be("1");
        QuestionType.MeaningToWord.Should().Be("2");
        QuestionType.Description.Should().Be("3");
        QuestionType.All.Should().BeEquivalentTo(new[] { "1", "2", "3" });
    }

    [Fact]
    public void Enum_Constant_Sets_Should_Contain_All_Defined_Values()
    {
        CefrLevel.All.Should().BeEquivalentTo(new[] { "A1", "A2", "B1", "B2", "C1", "C2" });
        TestMode.All.Should().BeEquivalentTo(new[] { "standard", "timed", "challenge", "elimination" });
        ScopeType.Values.Should().BeEquivalentTo(new[] { "all", "date_range", "start_date", "end_date", "wrong_words" });
        WordOrder.All.Should().BeEquivalentTo(new[] { "newest", "oldest", "random", "by_difficulty" });
        AnswerMethod.All.Should().BeEquivalentTo(new[] { "multiple_choice", "exact_typing", "ai_typing" });
        AddMethod.All.Should().BeEquivalentTo(new[] { "manual", "search", "random_topic", "random_synonym", "random_antonym" });
        UserStatus.All.Should().BeEquivalentTo(new[] { "active", "locked", "deleted" });
        UserRole.All.Should().BeEquivalentTo(new[] { "admin", "super_admin", "user" });
        AudioStatus.All.Should().BeEquivalentTo(new[] { "pending", "uploaded", "tts_generated", "missing", "deleted" });
    }

    [Fact]
    public void AppSettings_Should_Expose_Default_Configurable_Values()
    {
        AppSettings.MaxListsPerUser.Should().Be(50);
        AppSettings.AiPassThreshold.Should().Be(0.75);
        AppSettings.OtpCodeLength.Should().Be(6);
        AppSettings.OtpMaxVerifyAttempts.Should().Be(5);
        AppSettings.AccessTokenMinutes.Should().Be(15);
        AppSettings.RefreshTokenDays.Should().Be(30);
    }
}
