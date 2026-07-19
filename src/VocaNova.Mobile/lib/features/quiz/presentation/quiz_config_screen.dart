import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/widgets/offline_banner.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_state.dart';

const _questionLimitOptions = [10, 20, 30, 50, null];

const _modeLabels = {
  'standard': 'Standard',
  'timed': 'Timed',
  'challenge': 'Challenge',
  'elimination': 'Elimination',
};

const _answerLabels = {
  'multiple_choice': 'Multiple choice',
  'exact_typing': 'Typing',
  'ai_typing': 'AI typing',
};

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
      notifier.loadTopics();
    });
  }

  @override
  Widget build(BuildContext context) {
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
      appBar: AppBar(title: const Text('Cấu hình kiểm tra')),
      body: ListView(
        padding: const EdgeInsets.only(bottom: 16),
        children: [
          if (!isOnline) const OfflineBanner(),
          if (widget.initialListId != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
              child: Card(
                margin: EdgeInsets.zero,
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Text(
                    'Mở từ danh sách #${widget.initialListId}. '
                    'Bài kiểm tra chỉ lấy từ trong danh sách này, kết hợp với các bộ lọc bên dưới.',
                  ),
                ),
              ),
            ),
          _Section(title: 'Phạm vi từ vựng', child: _buildScope(state)),
          _Section(title: 'Chủ đề', child: _buildTopics(state)),
          _Section(title: 'Loại câu hỏi', child: _buildQuestionType(state)),
          _Section(title: 'Cách trả lời', child: _buildAnswerMethod(state)),
          _Section(title: 'Thứ tự', child: _buildWordOrder(state)),
          _Section(title: 'Chế độ', child: _buildMode(state)),
          _Section(title: 'Số câu hỏi', child: _buildQuestionLimit(state)),
          _SummaryBar(state: state),
        ],
      ),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
        child: Tooltip(
          message: isOnline ? '' : 'Cần kết nối mạng',
          child: FilledButton.icon(
            key: const Key('start-quiz-button'),
            onPressed: state.isCreating || !isOnline ? null : _startQuiz,
            icon: state.isCreating
                ? const SizedBox.square(
                    dimension: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.play_arrow),
            label: const Text('Bắt đầu'),
            style: FilledButton.styleFrom(
              minimumSize: const Size.fromHeight(52),
            ),
          ),
        ),
      ),
    );
  }

  QuizConfigNotifier get _notifier => ref.read(quizConfigProvider.notifier);

  Widget _buildScope(QuizConfigState state) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _choice(
              key: const Key('scope-all'),
              label: 'Tất cả từ vựng',
              selected: state.scopeType == 'all',
              onSelected: () => _notifier.setScope('all'),
            ),
            _choice(
              key: const Key('scope-this-week'),
              label: 'Tuần này',
              selected: state.scopeType == 'this_week',
              onSelected: () => _notifier.setScope('this_week'),
            ),
            _choice(
              key: const Key('scope-wrong-words'),
              label: 'Từ hay sai',
              selected: state.scopeType == 'wrong_words',
              onSelected: () => _notifier.setScope('wrong_words'),
            ),
            _choice(
              key: const Key('scope-date-range'),
              label: 'Khoảng ngày',
              selected: state.scopeType == 'date_range',
              onSelected: () => _notifier.setScope('date_range'),
            ),
          ],
        ),
        if (state.scopeType == 'date_range') ...[
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _DateButton(
                  key: const Key('date-from'),
                  label: 'Từ ngày',
                  date: state.dateFrom,
                  onTap: () => _pickDate(true),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _DateButton(
                  key: const Key('date-to'),
                  label: 'Đến ngày',
                  date: state.dateTo,
                  onTap: () => _pickDate(false),
                ),
              ),
            ],
          ),
        ],
      ],
    );
  }

  Widget _buildTopics(QuizConfigState state) {
    if (state.isLoadingTopics) {
      return const Center(child: CircularProgressIndicator());
    }
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        FilterChip(
          key: const Key('all-topics'),
          label: const Text('Tất cả chủ đề'),
          selected: state.selectedTopicIds.isEmpty,
          showCheckmark: false,
          onSelected: (_) => _notifier.clearTopics(),
        ),
        for (final topic in state.topics)
          FilterChip(
            key: Key('quiz-topic-${topic.topicId}'),
            label: Text(topic.displayName),
            selected: state.selectedTopicIds.contains(topic.topicId),
            showCheckmark: false,
            onSelected: (_) => _notifier.toggleTopic(topic.topicId),
          ),
      ],
    );
  }

  Widget _buildQuestionType(QuizConfigState state) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _choice(
          label: 'Word → meaning',
          selected: state.questionType == 1,
          onSelected: () => _notifier.setQuestionType(1),
        ),
        _choice(
          label: 'Meaning → word',
          selected: state.questionType == 2,
          onSelected: () => _notifier.setQuestionType(2),
        ),
        _choice(
          label: 'Mô tả → từ',
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
        for (final entry in _answerLabels.entries)
          _choice(
            label: entry.value,
            selected: state.answerMethod == entry.key,
            onSelected: () => _notifier.setAnswerMethod(entry.key),
          ),
      ],
    );
  }

  Widget _buildWordOrder(QuizConfigState state) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _choice(
          key: const Key('order-random'),
          label: 'Ngẫu nhiên',
          selected: state.wordOrder == 'random',
          onSelected: () => _notifier.setWordOrder('random'),
        ),
        _choice(
          key: const Key('order-newest'),
          label: 'Mới nhất',
          selected: state.wordOrder == 'newest',
          onSelected: () => _notifier.setWordOrder('newest'),
        ),
        _choice(
          key: const Key('order-oldest'),
          label: 'Cũ nhất',
          selected: state.wordOrder == 'oldest',
          onSelected: () => _notifier.setWordOrder('oldest'),
        ),
        _choice(
          key: const Key('order-by-difficulty'),
          label: 'Theo độ khó',
          selected: state.wordOrder == 'by_difficulty',
          onSelected: () => _notifier.setWordOrder('by_difficulty'),
        ),
      ],
    );
  }

  Widget _buildMode(QuizConfigState state) {
    return Column(
      children: [
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: _ModeCard(
                  value: 'standard',
                  title: 'Standard',
                  subtitle: 'Theo nhịp độ của bạn',
                  selected: state.mode == 'standard',
                  onTap: _notifier.setMode,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _ModeCard(
                  value: 'timed',
                  title: 'Timed',
                  subtitle: 'Chạy đua thời gian',
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
                  title: 'Challenge',
                  subtitle: 'Mạng và chuỗi đúng',
                  selected: state.mode == 'challenge',
                  onTap: _notifier.setMode,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _ModeCard(
                  value: 'elimination',
                  title: 'Elimination',
                  subtitle: 'Sai là kết thúc',
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
              decoration: const InputDecoration(labelText: 'Thời gian (giây)'),
              onChanged: (value) =>
                  _notifier.setTimeLimit(int.tryParse(value)),
            ),
          ),
        if (state.mode == 'elimination')
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: TextField(
              key: const Key('lives-input'),
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Số mạng'),
              onChanged: (value) => _notifier.setLives(int.tryParse(value)),
            ),
          ),
      ],
    );
  }

  Widget _buildQuestionLimit(QuizConfigState state) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final option in _questionLimitOptions)
          _choice(
            key: Key('question-limit-${option ?? 'all'}'),
            label: option?.toString() ?? 'Tất cả',
            selected: state.questionLimit == option,
            onSelected: () => _notifier.setQuestionLimit(option),
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
            title.toUpperCase(),
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
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: scheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SummaryBar extends StatelessWidget {
  const _SummaryBar({required this.state});

  final QuizConfigState state;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 4),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          _SummaryChip(
            label: 'Chế độ',
            value: _modeLabels[state.mode] ?? state.mode,
          ),
          _SummaryChip(
            label: 'Trả lời',
            value: _answerLabels[state.answerMethod] ?? state.answerMethod,
          ),
          _SummaryChip(
            label: 'Số câu',
            value: state.questionLimit?.toString() ?? 'Tất cả',
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
        color: theme.colorScheme.surfaceContainerHighest.withValues(
          alpha: 0.6,
        ),
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
