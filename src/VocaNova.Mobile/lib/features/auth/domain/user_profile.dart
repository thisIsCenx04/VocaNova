class UserProfile {
  const UserProfile({
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
  final LearningProfile? learningProfile;

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    final learningProfile = json['learning_profile'];
    return UserProfile(
      userId: json['user_id'] as int,
      phone: json['phone'] as String?,
      displayName: json['display_name'] as String,
      avatarUrl: json['avatar_url'] as String?,
      role: json['role'] as String,
      status: json['status'] as String,
      learningProfile: learningProfile is Map<String, dynamic>
          ? LearningProfile.fromJson(learningProfile)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'user_id': userId,
      'phone': phone,
      'display_name': displayName,
      'avatar_url': avatarUrl,
      'role': role,
      'status': status,
      'learning_profile': learningProfile?.toJson(),
    };
  }
}

class LearningProfile {
  const LearningProfile({
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

  factory LearningProfile.fromJson(Map<String, dynamic> json) {
    return LearningProfile(
      ageRangeId: json['age_range_id'] as int?,
      regionId: json['region_id'] as int?,
      occupationId: json['occupation_id'] as int?,
      educationLevelId: json['education_level_id'] as int?,
      learningPurposeId: json['learning_purpose_id'] as int?,
    );
  }

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
