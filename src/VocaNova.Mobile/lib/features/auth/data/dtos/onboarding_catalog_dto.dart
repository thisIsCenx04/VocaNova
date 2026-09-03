import 'package:vocanova_mobile/features/auth/domain/models/onboarding_catalog.dart';

class OnboardingOptionDto {
  const OnboardingOptionDto({required this.id, required this.label});

  final int id;
  final String label;

  factory OnboardingOptionDto.fromJson(Map<String, dynamic> json) {
    return OnboardingOptionDto(
      id: json['id'] as int,
      label: json['name'] as String,
    );
  }

  OnboardingOption toDomain() => OnboardingOption(id: id, label: label);
}

class LearningProfileOptionsDto {
  const LearningProfileOptionsDto({
    required this.ageRanges,
    required this.regions,
    required this.occupations,
    required this.educationLevels,
    required this.learningPurposes,
  });

  final List<OnboardingOptionDto> ageRanges;
  final List<OnboardingOptionDto> regions;
  final List<OnboardingOptionDto> occupations;
  final List<OnboardingOptionDto> educationLevels;
  final List<OnboardingOptionDto> learningPurposes;

  factory LearningProfileOptionsDto.fromJson(Map<String, dynamic> json) {
    return LearningProfileOptionsDto(
      ageRanges: _options(json['age_ranges']),
      regions: _options(json['regions']),
      occupations: _options(json['occupations']),
      educationLevels: _options(json['education_levels']),
      learningPurposes: _options(json['learning_purposes']),
    );
  }

  LearningProfileOptions toDomain() => LearningProfileOptions(
    ageRanges: ageRanges.map((dto) => dto.toDomain()).toList(growable: false),
    regions: regions.map((dto) => dto.toDomain()).toList(growable: false),
    occupations: occupations
        .map((dto) => dto.toDomain())
        .toList(growable: false),
    educationLevels: educationLevels
        .map((dto) => dto.toDomain())
        .toList(growable: false),
    learningPurposes: learningPurposes
        .map((dto) => dto.toDomain())
        .toList(growable: false),
  );

  static List<OnboardingOptionDto> _options(Object? value) {
    if (value is! List) {
      return const [];
    }
    return value
        .whereType<Map<String, dynamic>>()
        .map(OnboardingOptionDto.fromJson)
        .toList(growable: false);
  }
}
