/// A single lookup choice (age range, region, occupation, ...) served by the backend.
class OnboardingOption {
  const OnboardingOption({required this.id, required this.label});

  final int id;
  final String label;

  factory OnboardingOption.fromJson(Map<String, dynamic> json) {
    return OnboardingOption(
      id: json['id'] as int,
      label: json['name'] as String,
    );
  }
}

/// The lookup catalog behind the sign-up form and the onboarding questions.
///
/// These used to be hard-coded ids on the client, which silently broke whenever an
/// admin edited a lookup. They now come from `/api/recommendations/learning-profile-options`.
class LearningProfileOptions {
  const LearningProfileOptions({
    required this.ageRanges,
    required this.regions,
    required this.occupations,
    required this.educationLevels,
    required this.learningPurposes,
  });

  final List<OnboardingOption> ageRanges;
  final List<OnboardingOption> regions;
  final List<OnboardingOption> occupations;
  final List<OnboardingOption> educationLevels;
  final List<OnboardingOption> learningPurposes;

  static const empty = LearningProfileOptions(
    ageRanges: [],
    regions: [],
    occupations: [],
    educationLevels: [],
    learningPurposes: [],
  );

  factory LearningProfileOptions.fromJson(Map<String, dynamic> json) {
    return LearningProfileOptions(
      ageRanges: _options(json['age_ranges']),
      regions: _options(json['regions']),
      occupations: _options(json['occupations']),
      educationLevels: _options(json['education_levels']),
      learningPurposes: _options(json['learning_purposes']),
    );
  }

  static List<OnboardingOption> _options(Object? value) {
    if (value is! List) {
      return const [];
    }
    return value
        .whereType<Map<String, dynamic>>()
        .map(OnboardingOption.fromJson)
        .toList(growable: false);
  }
}
