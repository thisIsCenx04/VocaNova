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
}
