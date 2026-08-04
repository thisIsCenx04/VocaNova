using FluentAssertions;
using VocaNova.API.Common.Security;

namespace VocaNova.Tests.Shared;

public class SecurityHelperTests
{
    [Fact]
    public void PasswordHelper_Hash_Should_Create_BCrypt_Hash_With_Cost_12()
    {
        var hash = PasswordHelper.Hash("Password1!");

        hash.Should().NotBe("Password1!");
        hash.Should().StartWith("$2");
        hash.Split('$')[2].Should().Be("12");
    }

    [Fact]
    public void PasswordHelper_Verify_Should_Return_True_For_Correct_Password()
    {
        var hash = PasswordHelper.Hash("Password1!");

        var isValid = PasswordHelper.Verify("Password1!", hash);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void PasswordHelper_Verify_Should_Return_False_For_Wrong_Password()
    {
        var hash = PasswordHelper.Hash("Password1!");

        var isValid = PasswordHelper.Verify("WrongPassword1!", hash);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void PasswordHelper_Verify_Should_Return_False_For_Invalid_Hash()
    {
        var isValid = PasswordHelper.Verify("Password1!", "not-a-bcrypt-hash");

        isValid.Should().BeFalse();
    }

    [Fact]
    public void TokenHelper_HashSha256_Should_Return_Lowercase_Hex_Hash()
    {
        var hash = TokenHelper.HashSha256("refresh-token");

        hash.Should().Be("0eb17643d4e9261163783a420859c92c7d212fa9624106a12b510afbec266120");
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
