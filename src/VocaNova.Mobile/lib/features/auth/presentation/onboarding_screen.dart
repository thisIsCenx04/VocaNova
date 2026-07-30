import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/onboarding_catalog.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

/// Collects the intent half of the KNN profile vector.
///
/// The demographic half (age range, region, occupation, education level) is now gathered on
/// the sign-up form, so onboarding only asks what the learner wants — their purpose and the
/// topics they care about. Anything skipped here simply leaves that block of the vector at
/// zero; it never blocks the user from reaching the app.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  static const _stepCount = 2;

  int _step = 0;
  bool _isLoadingCatalog = true;
  bool _isSaving = false;
  String? _catalogError;

  List<OnboardingOption> _learningPurposes = const [];
  List<TopicSummary> _topics = const [];
  int? _learningPurposeId;
  final Set<int> _selectedTopicIds = <int>{};

  @override
  void initState() {
    super.initState();
    final profile = ref.read(authProvider).user?.learningProfile;
    _learningPurposeId = profile?.learningPurposeId;
    _loadCatalog();
  }

  Future<void> _loadCatalog() async {
    setState(() {
      _isLoadingCatalog = true;
      _catalogError = null;
    });
    try {
      final options = await ref
          .read(authRepositoryProvider)
          .getLearningProfileOptions();
      final topics = await ref.read(wordSearchRepositoryProvider).getTopics();
      if (!mounted) return;
      setState(() {
        _learningPurposes = options.learningPurposes;
        _topics = topics;
        _isLoadingCatalog = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _isLoadingCatalog = false;
        _catalogError = 'Không tải được danh mục. Vui lòng thử lại.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLoading =
        _isSaving || ref.watch(authProvider).status == AuthStatus.loading;

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
                _step == 0
                    ? 'Mục tiêu học từ vựng của bạn?'
                    : 'Bạn quan tâm chủ đề nào?',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 8),
              Text(
                _step == 0
                    ? 'VocaNova sẽ ưu tiên nội dung theo mục tiêu này.'
                    : 'Chọn ít nhất một chủ đề để nhận gợi ý từ vựng phù hợp.',
                style: Theme.of(context).textTheme.bodyLarge,
              ),
              const SizedBox(height: 24),
              Expanded(child: _stepBody(isLoading)),
              const SizedBox(height: 20),
              ElevatedButton(
                key: const Key('onboarding-next'),
                onPressed: isLoading || _isLoadingCatalog ? null : _next,
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

  Widget _stepBody(bool isLoading) {
    if (_isLoadingCatalog) {
      return const Center(child: CircularProgressIndicator());
    }
    final catalogError = _catalogError;
    if (catalogError != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(catalogError, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            TextButton(onPressed: _loadCatalog, child: const Text('Thử lại')),
          ],
        ),
      );
    }

    return SingleChildScrollView(
      child: Wrap(
        spacing: 10,
        runSpacing: 10,
        children: _step == 0
            ? [
                for (final option in _learningPurposes)
                  ChoiceChip(
                    key: Key('onboarding-purpose-${option.id}'),
                    label: Text(option.label),
                    selected: _learningPurposeId == option.id,
                    onSelected: isLoading
                        ? null
                        : (_) => setState(() => _learningPurposeId = option.id),
                  ),
              ]
            : [
                for (final topic in _topics)
                  FilterChip(
                    key: Key('onboarding-topic-${topic.topicId}'),
                    label: Text(topic.displayName),
                    selected: _selectedTopicIds.contains(topic.topicId),
                    onSelected: isLoading
                        ? null
                        : (selected) => setState(() {
                            if (selected) {
                              _selectedTopicIds.add(topic.topicId);
                            } else {
                              _selectedTopicIds.remove(topic.topicId);
                            }
                          }),
                  ),
              ],
      ),
    );
  }

  void _previous() {
    setState(() => _step--);
  }

  Future<void> _next() async {
    if (_step < _stepCount - 1) {
      setState(() => _step++);
      return;
    }

    setState(() => _isSaving = true);
    try {
      // The learning-profile endpoint replaces the whole row, so the demographic answers
      // captured at sign-up must be sent back untouched alongside the new purpose.
      final current = ref.read(authProvider).user?.learningProfile;
      final savedPurpose = await ref
          .read(authProvider.notifier)
          .updateLearningProfile(
            LearningProfile(
              ageRangeId: current?.ageRangeId,
              regionId: current?.regionId,
              occupationId: current?.occupationId,
              educationLevelId: current?.educationLevelId,
              learningPurposeId: _learningPurposeId,
            ),
          );
      if (!savedPurpose) {
        throw Exception(
          ref.read(authProvider).errorMessage ??
              'Không thể lưu thiết lập học tập.',
        );
      }

      await ref
          .read(authRepositoryProvider)
          .selectOnboardingTopics(_selectedTopicIds.toList(growable: false));

      if (!mounted) return;
      context.go(AppRoutes.home);
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            ref.read(authProvider).errorMessage ??
                'Không thể lưu thiết lập học tập. Vui lòng thử lại.',
          ),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }
}
