namespace VocaNova.API.Features.Auth.BLL.Models;

public sealed record RegisterCommand(
    string? Phone,
    string? Password,
    string? DisplayName,
    string? OtpCode,
    DateOnly? DateOfBirth,
    uint? RegionId,
    uint? OccupationId,
    uint? EducationLevelId);

public sealed record LoginCommand(string? Phone, string? Password);

public sealed record GoogleLoginCommand(string? IdToken);

public sealed record RefreshTokenCommand(string? RefreshToken);

public sealed record OtpSendCommand(string? Phone, string? Purpose);

public sealed record OtpVerifyCommand(string? Phone, string? OtpCode);

public sealed record ForgotPasswordCommand(string? Phone);

public sealed record ResetPasswordCommand(string? Phone, string? OtpCode, string? NewPassword);

public sealed record ChangePasswordCommand(string? CurrentPassword, string? NewPassword);

public sealed record UpdateProfileCommand(string? DisplayName, string? AvatarUrl);

public sealed record UpdateLearningProfileCommand(
    uint? AgeRangeId,
    uint? RegionId,
    uint? OccupationId,
    uint? EducationLevelId,
    uint? LearningPurposeId);

public sealed record UploadAvatarCommand(UploadedContent? Content);

public sealed record SignInContext(string? DeviceInfo, string? IpAddress);
