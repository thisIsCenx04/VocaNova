using System.Security.Cryptography;
using VocaNova.API.Common.Constants;

namespace VocaNova.API.Infrastructure.Otp;

public sealed class RandomOtpCodeGenerator : IOtpCodeGenerator
{
    private static readonly int MaxExclusive = (int)Math.Pow(10, AppSettings.OtpCodeLength);

    public string Generate()
    {
        var value = RandomNumberGenerator.GetInt32(0, MaxExclusive);
        return value.ToString($"D{AppSettings.OtpCodeLength}");
    }
}
