import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/widgets/offline_banner.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_icon.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_state.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

const _questionLimitOptions = [10, 20, 30, 50, null];

const _answerMethodKeys = ['multiple_choice', 'exact_typing', 'ai_typing'];

String _modeLabel(BuildContext context, String mode) {
  final l10n = AppLocalizations.of(context)!;
  switch (mode) {
    case 'standard':
      return l10n.quizModeStandard;
    case 'timed':
      return l10n.quizModeTimed;
    case 'challenge':
      return l10n.quizModeChallenge;
    case 'elimination':
      return l10n.quizModeElimination;
    default:
      return mode;
  }
}

String _answerLabel(BuildContext context, String method) {
  final l10n = AppLocalizations.of(context)!;
  switch (method) {
    case 'multiple_choice':
      return l10n.quizAnswerMultipleChoice;
    case 'exact_typing':
      return l10n.quizAnswerTyping;
    case 'ai_typing':
      return l10n.quizAnswerAiTyping;
    default:
      return method;
  }
}

class QuizConfigScreen extends ConsumerStatefulWidget {
  const QuizConfigScreen({this.initialListId, super.key});

  final int? initialListId;

  @override
  ConsumerState<QuizConfigScreen> createState() => _QuizConfigScreenState();
}

class _QuizConfigScreenState extends ConsumerState<QuizConfigScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      final notifier = ref.read(quizConfigProvider.notifier);
      notifier.setListId(widget.initialListId);
      notifier.loadSources();
    });
  }

  @override
  void didUpdateWidget(covariant QuizConfigScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Điều hướng lại vào màn này với danh sách khác (ví dụ "Bắt đầu kiểm tra"
    // từ một danh sách) thì cập nhật nguồn và tải lại số từ mới nhất.
    if (oldWidget.initialListId != widget.initialListId) {
      final notifier = ref.read(quizConfigProvider.notifier);
      notifier.setListId(widget.initialListId);
      notifier.loadSources();
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final state = ref.watch(quizConfigProvider);
    final isOnline = ref.watch(connectivityProvider).value ?? true;
    ref.listen(quizConfigProvider.select((value) => value.errorMessage), (
      previous,
      next,
    ) {
      if (next != null && next != previous) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next)));
      }
    });
    return Scaffold(
      appBar: AppBar(title: Text(l10n.quizConfigTitle)),
      body: ListView(
        padding: const EdgeInsets.only(bottom: 16),
        children: [
          if (!isOnline) const OfflineBanner(),
          _Section(
            title: l10n.quizConfigScopeSection,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _choice(
                      key: const Key('scope-all'),
                      label: l10n.quizConfigScopeAll,
                      selected: state.scopeType == 'all',
                      onSelected: () => _notifier.setScope('all'),
                    ),
                    _choice(
                      key: const Key('scope-start-date'),
                      label: l10n.quizConfigScopeFromDate,
                      selected: state.scopeType == 'start_date',
                      onSelected: () => _notifier.setScope('start_date'),
                    ),
                    _choice(
                      key: const Key('scope-end-date'),
                      label: l10n.quizConfigScopeToDate,
                      selected: state.scopeType == 'end_date',
                      onSelected: () => _notifier.setScope('end_date'),
                    ),
                    _choice(
                      key: const Key('scope-date-range'),
                      label: l10n.quizConfigScopeDateRange,
                      selected: state.scopeType == 'date_range',
                      onSelected: () => _notifier.setScope('date_range'),
                    ),
                    _choice(
                      key: const Key('scope-wrong-words'),
                      label: l10n.quizConfigScopeWrongWords,
                      selected: state.scopeType == 'wrong_words',
                      onSelected: () => _notifier.setScope('wrong_words'),
                    ),
                  ],
                ),
                if (state.scopeType == 'start_date' ||
                    state.scopeType == 'date_range') ...[
                  const SizedBox(height: 10),
                  _DateButton(
                    key: const Key('date-from'),
                    label: l10n.quizConfigDateFrom,
                    date: state.dateFrom,
                    onTap: () => _pickDate(true),
                  ),
                ],
                if (state.scopeType == 'end_date' ||
                    state.scopeType == 'date_range') ...[
                  const SizedBox(height: 10),
                  _DateButton(
                    key: const Key('date-to'),
                    label: l10n.quizConfigDateTo,
                    date: state.dateTo,
                    onTap: () => _pickDate(false),
                  ),
                ],
              ],
            ),
          ),
          _Section(
            title: l10n.quizConfigSourceSection,
            child: _QuizSourcePicker(
              state: state,
              onTypeChanged: _notifier.setSourceType,
              onSourceSelected: _notifier.selectSource,
            ),
          ),
          _Section(title: l10n.quizConfigModeSection, child: _buildMode(state)),
          _Section(
            title: l10n.quizConfigQuestionTypeSection,
            child: _buildQuestionType(state),
          ),
          _Section(
            title: l10n.quizConfigAnswerMethodSection,
            child: _buildAnswerMethod(state),
          ),
          _Section(
            title: l10n.quizConfigWordOrderSection,
            child: _buildWordOrder(state),
          ),
          _Section(
            title: l10n.quizConfigQuestionLimitSection,
            child: _buildQuestionLimit(state),
          ),
          _SummaryBar(
            state: state,
            effectiveLimit: _notifier.effectiveWordLimit(),
          ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
        child: Tooltip(
          message: isOnline ? '' : l10n.quizConfigNeedConnection,
          child: FilledButton.icon(
            key: const Key('start-quiz-button'),
            onPressed: state.isCreating || !isOnline ? null : _startQuiz,
            icon: state.isCreating
                ? const SizedBox.square(
                    dimension: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.play_arrow),
            label: Text(l10n.quizConfigStartButton),
            style: FilledButton.styleFrom(
              minimumSize: const Size.fromHeight(52),
            ),
          ),
        ),
      ),
    );
  }

  QuizConfigNotifier get _notifier => ref.read(quizConfigProvider.notifier);

  Widget _buildQuestionType(QuizConfigState state) {
    final l10n = AppLocalizations.of(context)!;
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _choice(
          label: l10n.quizConfigQuestionTypeWordToMeaning,
          selected: state.questionType == 1,
          onSelected: () => _notifier.setQuestionType(1),
        ),
        _choice(
          label: l10n.quizConfigQuestionTypeMeaningToWord,
          selected: state.questionType == 2,
          onSelected: () => _notifier.setQuestionType(2),
        ),
        _choice(
          label: l10n.quizConfigQuestionTypeDescToWord,
          selected: state.questionType == 3,
          onSelected: () => _notifier.setQuestionType(3),
        ),
      ],
    );
  }

  Widget _buildAnswerMethod(QuizConfigState state) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final key in _answerMethodKeys)
          _choice(
            label: _answerLabel(context, key),
            selected: state.answerMethod == key,
            onSelected: () => _notifier.setAnswerMethod(key),
          ),
      ],
    );
  }

  Widget _buildWordOrder(QuizConfigState state) {
    final l10n = AppLocalizations.of(context)!;
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _choice(
          key: const Key('order-random'),
          label: l10n.quizConfigOrderRandom,
          selected: state.wordOrder == 'random',
          onSelected: () => _notifier.setWordOrder('random'),
        ),
        _choice(
          key: const Key('order-newest'),
          label: l10n.quizConfigOrderNewest,
          selected: state.wordOrder == 'newest',
          onSelected: () => _notifier.setWordOrder('newest'),
        ),
        _choice(
          key: const Key('order-oldest'),
          label: l10n.quizConfigOrderOldest,
          selected: state.wordOrder == 'oldest',
          onSelected: () => _notifier.setWordOrder('oldest'),
        ),
        _choice(
          key: const Key('order-by-difficulty'),
          label: l10n.quizConfigOrderByDifficulty,
          selected: state.wordOrder == 'by_difficulty',
          onSelected: () => _notifier.setWordOrder('by_difficulty'),
        ),
      ],
    );
  }

  Widget _buildMode(QuizConfigState state) {
    final l10n = AppLocalizations.of(context)!;
    return Column(
      children: [
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: _ModeCard(
                  value: 'standard',
                  title: l10n.quizModeStandard,
                  subtitle: l10n.quizConfigModeStandardSubtitle,
                  selected: state.mode == 'standard',
                  onTap: _notifier.setMode,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _ModeCard(
                  value: 'timed',
                  title: l10n.quizModeTimed,
                  subtitle: l10n.quizConfigModeTimedSubtitle,
                  selected: state.mode == 'timed',
                  onTap: _notifier.setMode,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 10),
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: _ModeCard(
                  value: 'challenge',
                  title: l10n.quizModeChallenge,
                  subtitle: l10n.quizConfigModeChallengeSubtitle,
                  selected: state.mode == 'challenge',
                  onTap: _notifier.setMode,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _ModeCard(
                  value: 'elimination',
                  title: l10n.quizModeElimination,
                  subtitle: l10n.quizConfigModeEliminationSubtitle,
                  selected: state.mode == 'elimination',
                  onTap: _notifier.setMode,
                ),
              ),
            ],
          ),
        ),
        if (state.mode == 'timed')
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: TextField(
              key: const Key('time-limit-input'),
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: l10n.quizConfigTimeLimitLabel,
              ),
              onChanged: (value) => _notifier.setTimeLimit(int.tryParse(value)),
            ),
          ),
        if (state.mode == 'elimination')
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: TextField(
              key: const Key('lives-input'),
              keyboardType: TextInputType.number,
              decoration: InputDecoration(labelText: l10n.quizConfigLivesLabel),
              onChanged: (value) => _notifier.setLives(int.tryParse(value)),
            ),
          ),
      ],
    );
  }

  Widget _buildQuestionLimit(QuizConfigState state) {
    final wordCount = _notifier.selectedSourceWordCount();
    final l10n = AppLocalizations.of(context)!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final option in _questionLimitOptions)
              _choice(
                key: Key('question-limit-${option ?? 'all'}'),
                label: option?.toString() ?? l10n.quizConfigLimitAll,
                selected:
                    !state.useCustomLimit && state.questionLimit == option,
                onSelected: () => _notifier.setQuestionLimit(option),
              ),
            _choice(
              key: const Key('question-limit-custom'),
              label: l10n.quizConfigLimitCustom,
              selected: state.useCustomLimit,
              onSelected: _notifier.useCustomQuestionLimit,
            ),
          ],
        ),
        if (state.useCustomLimit)
          Padding(
            padding: const EdgeInsets.only(top: 12),
            child: TextField(
              key: const Key('custom-question-input'),
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: l10n.quizConfigCustomLimitLabel,
                helperText: wordCount != null
                    ? l10n.quizConfigCustomLimitHelper(wordCount)
                    : l10n.quizConfigCustomLimitHelperEmpty,
              ),
              onChanged: (value) =>
                  _notifier.setCustomQuestionLimit(int.tryParse(value)),
            ),
          ),
      ],
    );
  }

  Widget _choice({
    required String label,
    required bool selected,
    required VoidCallback onSelected,
    Key? key,
  }) {
    return ChoiceChip(
      key: key,
      label: Text(label),
      selected: selected,
      showCheckmark: false,
      onSelected: (_) => onSelected(),
    );
  }

  Future<void> _pickDate(bool from) async {
    final state = ref.read(quizConfigProvider);
    final selected = await showDatePicker(
      context: context,
      initialDate: from
          ? state.dateFrom ?? DateTime.now()
          : state.dateTo ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (selected == null) return;
    from ? _notifier.setDateFrom(selected) : _notifier.setDateTo(selected);
  }

  Future<void> _startQuiz() async {
    final session = await _notifier.createSession();
    if (session != null && mounted) {
      context.push(
        AppRoutes.quizSession(session.sessionId.toString()),
        extra: session,
      );
    }
  }
}

class _QuizSourcePicker extends StatelessWidget {
  const _QuizSourcePicker({
    required this.state,
    required this.onTypeChanged,
    required this.onSourceSelected,
  });

  final QuizConfigState state;
  final ValueChanged<String> onTypeChanged;
  final ValueChanged<int> onSourceSelected;

  @override
  Widget build(BuildContext context) {
    if (state.isLoadingSources) {
      return const Center(child: CircularProgressIndicator());
    }

    final showingLists = state.sourceType == QuizSourceType.myList;
    final items = showingLists
        ? state.lists
              .map(
                (list) => _SourceItem(
                  id: list.listId,
                  name: list.listName,
                  wordCount: list.wordCount,
                  icon: Icons.bookmark_outline,
                ),
              )
              .toList(growable: false)
        : state.personalTopics
              .map(
                (topic) => _SourceItem(
                  id: topic.listId,
                  name: topic.localizedName(context),
                  wordCount: topic.wordCount,
                  icon: topicIconData(topic.icon, topic.name),
                ),
              )
              .toList(growable: false);

    final l10n = AppLocalizations.of(context)!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          height: 46,
          padding: const EdgeInsets.all(4),
          decoration: BoxDecoration(
            color: const Color(0xFFF0EDF4),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Row(
            children: [
              Expanded(
                child: _SourceTypeButton(
                  key: const Key('quiz-source-my-list'),
                  label: l10n.quizConfigSourceMyList,
                  icon: Icons.bookmark_outline,
                  selected: showingLists,
                  onTap: () => onTypeChanged(QuizSourceType.myList),
                ),
              ),
              Expanded(
                child: _SourceTypeButton(
                  key: const Key('quiz-source-personal-topic'),
                  label: l10n.quizConfigSourcePersonalTopic,
                  icon: Icons.auto_stories_outlined,
                  selected: !showingLists,
                  onTap: () => onTypeChanged(QuizSourceType.personalTopic),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Text(
          showingLists
              ? l10n.quizConfigSourceHintList
              : l10n.quizConfigSourceHintTopic,
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: const Color(0xFF706A76)),
        ),
        const SizedBox(height: 10),
        if (items.isEmpty)
          _SourceEmptyState(personalTopic: !showingLists)
        else
          for (final item in items) ...[
            _SourceCard(
              item: item,
              sourceType: state.sourceType,
              selected: item.id != null && item.id == state.listId,
              onTap: item.id == null || item.wordCount == 0
                  ? null
                  : () => onSourceSelected(item.id!),
            ),
            const SizedBox(height: 8),
          ],
      ],
    );
  }
}

class _SourceItem {
  const _SourceItem({
    required this.id,
    required this.name,
    required this.wordCount,
    required this.icon,
  });

  final int? id;
  final String name;
  final int wordCount;
  final IconData icon;
}

class _SourceTypeButton extends StatelessWidget {
  const _SourceTypeButton({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
    super.key,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? Colors.white : Colors.transparent,
      borderRadius: BorderRadius.circular(10),
      elevation: selected ? 1 : 0,
      child: InkWell(
        borderRadius: BorderRadius.circular(10),
        onTap: onTap,
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              icon,
              size: 17,
              color: selected ? AppColors.primary : const Color(0xFF77717D),
            ),
            const SizedBox(width: 6),
            Flexible(
              child: Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
                  color: selected
                      ? const Color(0xFF2D2634)
                      : const Color(0xFF77717D),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SourceCard extends StatelessWidget {
  const _SourceCard({
    required this.item,
    required this.sourceType,
    required this.selected,
    required this.onTap,
  });

  final _SourceItem item;
  final String sourceType;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final disabled = onTap == null;
    final kind = sourceType == QuizSourceType.myList ? 'list' : 'topic';
    return Material(
      color: selected
          ? AppColors.primary.withValues(alpha: 0.09)
          : const Color(0xFFFAF9FB),
      borderRadius: BorderRadius.circular(13),
      child: InkWell(
        key: Key('quiz-source-$kind-${item.id ?? item.name}'),
        borderRadius: BorderRadius.circular(13),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 11),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(13),
            border: Border.all(
              color: selected ? AppColors.primary : const Color(0xFFE1DCE5),
              width: selected ? 1.5 : 1,
            ),
          ),
          child: Row(
            children: [
              Container(
                width: 36,
                height: 36,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: const Color(0xFFEDE7FF),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(item.icon, size: 18, color: AppColors.primary),
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: disabled
                            ? const Color(0xFF928C96)
                            : const Color(0xFF28232D),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      item.wordCount == 0
                          ? AppLocalizations.of(
                              context,
                            )!.quizConfigSourceNoWords
                          : AppLocalizations.of(
                              context,
                            )!.quizConfigSourceWordCount(item.wordCount),
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: disabled
                            ? const Color(0xFFA19AA5)
                            : AppColors.primary,
                      ),
                    ),
                  ],
                ),
              ),
              Icon(
                selected ? Icons.radio_button_checked : Icons.radio_button_off,
                color: disabled
                    ? const Color(0xFFC9C4CC)
                    : selected
                    ? AppColors.primary
                    : const Color(0xFF8D8791),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SourceEmptyState extends StatelessWidget {
  const _SourceEmptyState({required this.personalTopic});

  final bool personalTopic;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFF8F6FA),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        personalTopic
            ? AppLocalizations.of(context)!.quizConfigEmptyTopicHint
            : AppLocalizations.of(context)!.quizConfigEmptyListHint,
        textAlign: TextAlign.center,
        style: Theme.of(context).textTheme.bodySmall,
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
      decoration: BoxDecoration(
        border: Border(
          bottom: BorderSide(color: theme.dividerColor.withValues(alpha: 0.4)),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: theme.textTheme.labelSmall?.copyWith(
              fontWeight: FontWeight.w700,
              letterSpacing: 0.8,
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 12),
          child,
        ],
      ),
    );
  }
}

class _ModeCard extends StatelessWidget {
  const _ModeCard({
    required this.value,
    required this.title,
    required this.subtitle,
    required this.selected,
    required this.onTap,
  });

  final String value;
  final String title;
  final String subtitle;
  final bool selected;
  final ValueChanged<String> onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return InkWell(
      key: Key('quiz-mode-$value'),
      borderRadius: BorderRadius.circular(12),
      onTap: () => onTap(value),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(12),
          color: selected ? scheme.primary.withValues(alpha: 0.08) : null,
          border: Border.all(
            color: selected ? scheme.primary : scheme.outlineVariant,
            width: selected ? 1.5 : 1,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: TextStyle(
                fontWeight: FontWeight.w700,
                color: selected ? scheme.primary : null,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              subtitle,
              style: Theme.of(
                context,
              ).textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
            ),
          ],
        ),
      ),
    );
  }
}

class _SummaryBar extends StatelessWidget {
  const _SummaryBar({required this.state, required this.effectiveLimit});

  final QuizConfigState state;
  final int? effectiveLimit;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 4),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          _SummaryChip(
            label: l10n.quizConfigSummaryMode,
            value: _modeLabel(context, state.mode),
          ),
          _SummaryChip(
            label: l10n.quizConfigSummaryAnswer,
            value: _answerLabel(context, state.answerMethod),
          ),
          _SummaryChip(
            label: l10n.quizConfigSummaryCount,
            // Hiển thị đúng số sẽ kiểm tra sau khi giới hạn theo tổng số từ.
            value: state.questionLimit == null
                ? (state.useCustomLimit
                      ? l10n.quizConfigLimitCustom
                      : l10n.quizConfigLimitAll)
                : (effectiveLimit?.toString() ??
                      state.questionLimit.toString()),
          ),
        ],
      ),
    );
  }
}

class _SummaryChip extends StatelessWidget {
  const _SummaryChip({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.6),
      ),
      child: Text.rich(
        TextSpan(
          children: [
            TextSpan(
              text: '$label ',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            TextSpan(
              text: value,
              style: theme.textTheme.bodySmall?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DateButton extends StatelessWidget {
  const _DateButton({
    required this.label,
    required this.date,
    required this.onTap,
    super.key,
  });

  final String label;
  final DateTime? date;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: onTap,
      icon: const Icon(Icons.calendar_month, size: 18),
      label: Text(
        date == null
            ? label
            : '$label: ${DateFormat('dd/MM/yyyy').format(date!)}',
        overflow: TextOverflow.ellipsis,
      ),
    );
  }
}
