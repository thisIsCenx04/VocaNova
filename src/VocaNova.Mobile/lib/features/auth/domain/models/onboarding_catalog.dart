/// A single lookup choice (age range, region, occupation, ...) served by the backend.
class OnboardingOption {
  const OnboardingOption({required this.id, required this.label});

  final int id;
  final String label;
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
}
