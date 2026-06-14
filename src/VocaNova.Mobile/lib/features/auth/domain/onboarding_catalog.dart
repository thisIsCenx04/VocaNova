class OnboardingOption {
  const OnboardingOption({required this.id, required this.label});

  final int id;
  final String label;
}

abstract final class OnboardingCatalog {
  // Replace this catalog with backend lookups when a public lookup API exists.
  static const ageRanges = [
    OnboardingOption(id: 1, label: 'Dưới 18 tuổi'),
    OnboardingOption(id: 2, label: '18 - 24 tuổi'),
    OnboardingOption(id: 3, label: '25 - 34 tuổi'),
    OnboardingOption(id: 4, label: '35 - 44 tuổi'),
    OnboardingOption(id: 5, label: 'Từ 45 tuổi'),
  ];

  static const regions = [
    OnboardingOption(id: 1, label: 'Miền Bắc'),
    OnboardingOption(id: 2, label: 'Miền Trung'),
    OnboardingOption(id: 3, label: 'Miền Nam'),
  ];

  static const occupations = [
    OnboardingOption(id: 1, label: 'Học sinh'),
    OnboardingOption(id: 2, label: 'Sinh viên'),
    OnboardingOption(id: 3, label: 'Nhân viên văn phòng'),
    OnboardingOption(id: 4, label: 'Kinh doanh tự do'),
    OnboardingOption(id: 5, label: 'Khác'),
  ];

  static const educationLevels = [
    OnboardingOption(id: 1, label: 'Trung học cơ sở'),
    OnboardingOption(id: 2, label: 'Trung học phổ thông'),
    OnboardingOption(id: 3, label: 'Cao đẳng'),
    OnboardingOption(id: 4, label: 'Đại học'),
    OnboardingOption(id: 5, label: 'Sau đại học'),
  ];

  static const learningPurposes = [
    OnboardingOption(id: 1, label: 'Giao tiếp hằng ngày'),
    OnboardingOption(id: 2, label: 'Học tập và thi cử'),
    OnboardingOption(id: 3, label: 'Công việc'),
    OnboardingOption(id: 4, label: 'Du lịch'),
    OnboardingOption(id: 5, label: 'Phát triển bản thân'),
  ];
}
