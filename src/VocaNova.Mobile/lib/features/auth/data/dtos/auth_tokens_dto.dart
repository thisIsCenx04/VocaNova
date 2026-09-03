import 'package:vocanova_mobile/features/auth/domain/models/auth_tokens.dart';

class AuthTokensDto {
  const AuthTokensDto({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.tokenType,
  });

  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final String tokenType;

  factory AuthTokensDto.fromJson(Map<String, dynamic> json) {
    return AuthTokensDto(
      accessToken: json['access_token'] as String,
      refreshToken: json['refresh_token'] as String,
      expiresIn: json['expires_in'] as int,
      tokenType: json['token_type'] as String? ?? 'Bearer',
    );
  }

  AuthTokens toDomain() => AuthTokens(
    accessToken: accessToken,
    refreshToken: refreshToken,
    expiresIn: expiresIn,
    tokenType: tokenType,
  );
}
