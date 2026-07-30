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
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

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
  bool _hasCatalogError = false;

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
      _hasCatalogError = false;
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
        _hasCatalogError = true;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLoading =
        _isSaving || ref.watch(authProvider).status == AuthStatus.loading;
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        leading: _step == 0
            ? null
            : IconButton(
                tooltip: l10n.authBackButton,
                onPressed: isLoading ? null : _previous,
                icon: const Icon(Icons.arrow_back),
              ),
        title: Text(l10n.authOnboardingTitle),
        actions: [
          TextButton(
            key: const Key('onboarding-skip'),
            onPressed: isLoading ? null : () => context.go(AppRoutes.home),
            child: Text(l10n.authSkipAction),
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
                l10n.authStepProgress(_step + 1, _stepCount),
                key: const Key('onboarding-progress'),
                textAlign: TextAlign.right,
                style: Theme.of(context).textTheme.bodySmall,
              ),
              const SizedBox(height: 28),
              Text(
                _step == 0
                    ? l10n.authOnboardingGoalHeadline
                    : l10n.authOnboardingTopicsHeadline,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 8),
              Text(
                _step == 0
                    ? l10n.authOnboardingGoalSubtitle
                    : l10n.authOnboardingTopicsSubtitle,
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
                    : Text(
                        _step == _stepCount - 1
                            ? l10n.authOnboardingFinishButton
                            : l10n.authOnboardingContinueButton,
                      ),
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
    if (_hasCatalogError) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              AppLocalizations.of(context)!.authCatalogLoadError,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 12),
            TextButton(
              onPressed: _loadCatalog,
              child: Text(AppLocalizations.of(context)!.authRetryButton),
            ),
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
                    label: Text(topic.localizedName(context)),
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

    final l10n = AppLocalizations.of(context)!;
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
              l10n.authLearningProfileSaveFailed,
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
                l10n.authLearningProfileSaveFailedRetry,
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
