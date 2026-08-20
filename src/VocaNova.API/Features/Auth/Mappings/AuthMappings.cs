using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.Contracts.Requests;
using VocaNova.API.Features.Auth.Contracts.Responses;

namespace VocaNova.API.Features.Auth.Mappings;

public static class AuthMappings
{
    public static RegisterCommand ToCommand(this RegisterRequest request) =>
        new(
            request.Phone,
            request.Password,
            request.DisplayName,
            request.OtpCode,
            request.DateOfBirth,
            request.RegionId,
            request.OccupationId,
            request.EducationLevelId);

    public static LoginCommand ToCommand(this LoginRequest request) => new(request.Phone, request.Password);

    public static GoogleLoginCommand ToCommand(this GoogleLoginRequest request) => new(request.IdToken);

    public static RefreshTokenCommand ToCommand(this RefreshTokenRequest request) => new(request.RefreshToken);

    public static OtpSendCommand ToCommand(this OtpSendRequest request) => new(request.Phone, request.Purpose);

    public static OtpVerifyCommand ToCommand(this OtpVerifyRequest request) => new(request.Phone, request.OtpCode);

    public static ForgotPasswordCommand ToCommand(this ForgotPasswordRequest request) => new(request.Phone);

    public static ResetPasswordCommand ToCommand(this ResetPasswordRequest request) =>
        new(request.Phone, request.OtpCode, request.NewPassword);

    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request) =>
        new(request.CurrentPassword, request.NewPassword);

    public static UpdateProfileCommand ToCommand(this UpdateUserProfileRequest request) =>
        new(request.DisplayName, request.AvatarUrl);

    public static UpdateLearningProfileCommand ToCommand(this UpdateLearningProfileRequest request) =>
        new(
            request.AgeRangeId,
            request.RegionId,
            request.OccupationId,
            request.EducationLevelId,
            request.LearningPurposeId);

    public static UploadedContent? ToUploadedContent(this UploadAvatarRequest request, Stream? stream, uint ownerId) =>
        request.File is null || stream is null
            ? null
            : new UploadedContent(
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                stream,
                ownerId);

    public static TokenResponse ToResponse(this AuthTokenPair token) =>
        new(token.AccessToken, token.RefreshToken, token.ExpiresIn, token.TokenType);

    public static OtpSendResponse ToResponse(this OtpSendResult result) => new(result.ExpiresIn);

    public static OtpVerifyResponse ToResponse(this OtpVerificationResult result) => new(result.Verified);

    public static UserProfileResponse ToResponse(this UserProfile profile) =>
        new(
            profile.UserId,
            profile.Phone,
            profile.FullName,
            profile.AvatarUrl,
            profile.Role,
            profile.Status,
            profile.LearningProfile?.ToResponse());

    public static LearningProfileResponse ToResponse(this LearningProfile profile) =>
        new(
            profile.AgeRangeId,
            profile.RegionId,
            profile.OccupationId,
            profile.EducationLevelId,
            profile.LearningPurposeId);
}
