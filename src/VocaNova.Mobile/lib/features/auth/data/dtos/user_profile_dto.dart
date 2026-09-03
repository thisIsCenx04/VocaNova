import 'package:vocanova_mobile/features/auth/domain/models/user_profile.dart';

class UserProfileDto {
  const UserProfileDto({
    required this.userId,
    required this.displayName,
    required this.role,
    required this.status,
    this.phone,
    this.avatarUrl,
    this.learningProfile,
  });

  final int userId;
  final String? phone;
  final String displayName;
  final String? avatarUrl;
  final String role;
  final String status;
  final LearningProfileDto? learningProfile;

  factory UserProfileDto.fromJson(Map<String, dynamic> json) {
    final learningProfile = json['learning_profile'];
    return UserProfileDto(
      userId: json['user_id'] as int,
      phone: json['phone'] as String?,
      displayName: json['display_name'] as String,
      avatarUrl: json['avatar_url'] as String?,
      role: json['role'] as String,
      status: json['status'] as String,
      learningProfile: learningProfile is Map<String, dynamic>
          ? LearningProfileDto.fromJson(learningProfile)
          : null,
    );
  }

  factory UserProfileDto.fromDomain(UserProfile profile) => UserProfileDto(
    userId: profile.userId,
    phone: profile.phone,
    displayName: profile.displayName,
    avatarUrl: profile.avatarUrl,
    role: profile.role,
    status: profile.status,
    learningProfile: profile.learningProfile == null
        ? null
        : LearningProfileDto.fromDomain(profile.learningProfile!),
  );

  UserProfile toDomain() => UserProfile(
    userId: userId,
    phone: phone,
    displayName: displayName,
    avatarUrl: avatarUrl,
    role: role,
    status: status,
    learningProfile: learningProfile?.toDomain(),
  );

  Map<String, dynamic> toJson() => {
    'user_id': userId,
    'phone': phone,
    'display_name': displayName,
    'avatar_url': avatarUrl,
    'role': role,
    'status': status,
    'learning_profile': learningProfile?.toJson(),
  };
}

class LearningProfileDto {
  const LearningProfileDto({
    this.ageRangeId,
    this.regionId,
    this.occupationId,
    this.educationLevelId,
    this.learningPurposeId,
  });

  final int? ageRangeId;
  final int? regionId;
  final int? occupationId;
  final int? educationLevelId;
  final int? learningPurposeId;

  factory LearningProfileDto.fromJson(Map<String, dynamic> json) {
    return LearningProfileDto(
      ageRangeId: json['age_range_id'] as int?,
      regionId: json['region_id'] as int?,
      occupationId: json['occupation_id'] as int?,
      educationLevelId: json['education_level_id'] as int?,
      learningPurposeId: json['learning_purpose_id'] as int?,
    );
  }

  factory LearningProfileDto.fromDomain(LearningProfile profile) {
    return LearningProfileDto(
      ageRangeId: profile.ageRangeId,
      regionId: profile.regionId,
      occupationId: profile.occupationId,
      educationLevelId: profile.educationLevelId,
      learningPurposeId: profile.learningPurposeId,
    );
  }

  LearningProfile toDomain() => LearningProfile(
    ageRangeId: ageRangeId,
    regionId: regionId,
    occupationId: occupationId,
    educationLevelId: educationLevelId,
    learningPurposeId: learningPurposeId,
  );

  Map<String, dynamic> toJson() {
    return {
      'age_range_id': ageRangeId,
      'region_id': regionId,
      'occupation_id': occupationId,
      'education_level_id': educationLevelId,
      'learning_purpose_id': learningPurposeId,
    };
  }
}
