import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/onboarding_catalog.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';

class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  static const _stepCount = 5;

  int _step = 0;
  int? _ageRangeId;
  int? _regionId;
  int? _occupationId;
  int? _educationLevelId;
  int? _learningPurposeId;

  @override
  void initState() {
    super.initState();
    final profile = ref.read(authProvider).user?.learningProfile;
    _ageRangeId = profile?.ageRangeId;
    _regionId = profile?.regionId;
    _occupationId = profile?.occupationId;
    _educationLevelId = profile?.educationLevelId;
    _learningPurposeId = profile?.learningPurposeId;
  }

  @override
  Widget build(BuildContext context) {
    final isLoading = ref.watch(authProvider).status == AuthStatus.loading;
    final data = _steps[_step];

    return Scaffold(
      appBar: AppBar(
        leading: _step == 0
            ? null
            : IconButton(
                tooltip: 'Quay lại',
                onPressed: isLoading ? null : _previous,
                icon: const Icon(Icons.arrow_back),
              ),
        title: const Text('Thiết lập học tập'),
        actions: [
          TextButton(
            key: const Key('onboarding-skip'),
            onPressed: isLoading ? null : () => context.go(AppRoutes.home),
            child: const Text('Bỏ qua'),
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              LinearProgressIndicator(value: (_step + 1) / _stepCount),
              const SizedBox(height: 10),
              Text(
                'Bước ${_step + 1}/$_stepCount',
                key: const Key('onboarding-progress'),
                textAlign: TextAlign.right,
                style: Theme.of(context).textTheme.bodySmall,
              ),
              const SizedBox(height: 28),
              Text(
                data.title,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 8),
              Text(data.subtitle, style: Theme.of(context).textTheme.bodyLarge),
              const SizedBox(height: 24),
              Expanded(
                child: SingleChildScrollView(
                  child: Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children: [
                      for (final option in data.options)
                        ChoiceChip(
                          key: Key('onboarding-$_step-${option.id}'),
                          label: Text(option.label),
                          selected: data.selectedId == option.id,
                          onSelected: isLoading
                              ? null
                              : (_) => setState(() => data.select(option.id)),
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                key: const Key('onboarding-next'),
                onPressed: isLoading ? null : _next,
                child: isLoading
                    ? const SizedBox.square(
                        dimension: 22,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(_step == _stepCount - 1 ? 'Hoàn tất' : 'Tiếp tục'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  List<_OnboardingStep> get _steps => [
    _OnboardingStep(
      title: 'Độ tuổi của bạn?',
      subtitle: 'Giúp VocaNova lựa chọn nội dung phù hợp.',
      options: OnboardingCatalog.ageRanges,
      selectedId: _ageRangeId,
      select: (id) => _ageRangeId = id,
    ),
    _OnboardingStep(
      title: 'Bạn đang sống ở khu vực nào?',
      subtitle: 'Khu vực giúp cá nhân hóa gợi ý học tập.',
      options: OnboardingCatalog.regions,
      selectedId: _regionId,
      select: (id) => _regionId = id,
    ),
    _OnboardingStep(
      title: 'Công việc hiện tại của bạn?',
      subtitle: 'Chọn lựa chọn gần nhất với bạn.',
      options: OnboardingCatalog.occupations,
      selectedId: _occupationId,
      select: (id) => _occupationId = id,
    ),
    _OnboardingStep(
      title: 'Trình độ học vấn của bạn?',
      subtitle: 'Thông tin này hoàn toàn có thể cập nhật sau.',
      options: OnboardingCatalog.educationLevels,
      selectedId: _educationLevelId,
      select: (id) => _educationLevelId = id,
    ),
    _OnboardingStep(
      title: 'Mục tiêu học từ vựng của bạn?',
      subtitle: 'VocaNova sẽ ưu tiên nội dung theo mục tiêu này.',
      options: OnboardingCatalog.learningPurposes,
      selectedId: _learningPurposeId,
      select: (id) => _learningPurposeId = id,
    ),
  ];

  void _previous() {
    setState(() => _step--);
  }

  Future<void> _next() async {
    if (_step < _stepCount - 1) {
      setState(() => _step++);
      return;
    }

    final success = await ref
        .read(authProvider.notifier)
        .updateLearningProfile(
          LearningProfile(
            ageRangeId: _ageRangeId,
            regionId: _regionId,
            occupationId: _occupationId,
            educationLevelId: _educationLevelId,
            learningPurposeId: _learningPurposeId,
          ),
        );
    if (!mounted) return;
    if (success) {
      context.go(AppRoutes.home);
      return;
    }
    final message =
        ref.read(authProvider).errorMessage ??
        'Không thể lưu thiết lập học tập. Vui lòng thử lại.';
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

class _OnboardingStep {
  const _OnboardingStep({
    required this.title,
    required this.subtitle,
    required this.options,
    required this.selectedId,
    required this.select,
  });

  final String title;
  final String subtitle;
  final List<OnboardingOption> options;
  final int? selectedId;
  final ValueChanged<int> select;
}
