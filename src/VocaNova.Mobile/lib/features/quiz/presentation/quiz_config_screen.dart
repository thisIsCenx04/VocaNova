import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/widgets/offline_banner.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_state.dart';

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
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
        children: [
          if (!isOnline) const OfflineBanner(),
          _Section(
            title: 'Phạm vi từ',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _choice(
                      label: 'Tất cả',
                      selected: state.scopeType == 'all',
                      onSelected: () => _notifier.setScope('all'),
                    ),
                    _choice(
                      label: 'Từ ngày',
                      selected: state.scopeType == 'start_date',
                      onSelected: () => _notifier.setScope('start_date'),
                    ),
                    _choice(
                      label: 'Đến ngày',
                      selected: state.scopeType == 'end_date',
                      onSelected: () => _notifier.setScope('end_date'),
                    ),
                    _choice(
                      label: 'Khoảng ngày',
                      selected: state.scopeType == 'date_range',
                      onSelected: () => _notifier.setScope('date_range'),
                    ),
                  ],
                ),
                if (state.scopeType == 'start_date' ||
                    state.scopeType == 'date_range') ...[
                  const SizedBox(height: 10),
                  _DateButton(
                    key: const Key('date-from'),
                    label: 'Ngày bắt đầu',
                    date: state.dateFrom,
                    onTap: () => _pickDate(true),
                  ),
                ],
                if (state.scopeType == 'end_date' ||
                    state.scopeType == 'date_range') ...[
                  const SizedBox(height: 10),
                  _DateButton(
                    key: const Key('date-to'),
                    label: 'Ngày kết thúc',
                    date: state.dateTo,
                    onTap: () => _pickDate(false),
                  ),
                ],
              ],
            ),
          ),
          _Section(
            title: 'Nguồn kiểm tra',
            child: _QuizSourcePicker(
              state: state,
              onTypeChanged: _notifier.setSourceType,
              onSourceSelected: _notifier.selectSource,
            ),
          ),
          _Section(
            title: 'Chế độ',
            child: Column(
              children: [
                _ModeTile(
                  value: 'standard',
                  title: 'Standard',
                  description: 'Kiểm tra thông thường, không giới hạn.',
                  selected: state.mode == 'standard',
                  onTap: _notifier.setMode,
                ),
                _ModeTile(
                  value: 'timed',
                  title: 'Timed',
                  description: 'Hoàn thành trước khi hết thời gian.',
                  selected: state.mode == 'timed',
                  onTap: _notifier.setMode,
                ),
                _ModeTile(
                  value: 'challenge',
                  title: 'Challenge',
                  description: 'Thử thách với thứ tự từ ngẫu nhiên.',
                  selected: state.mode == 'challenge',
                  onTap: _notifier.setMode,
                ),
                _ModeTile(
                  value: 'elimination',
                  title: 'Elimination',
                  description: 'Bài kiểm tra kết thúc khi hết mạng.',
                  selected: state.mode == 'elimination',
                  onTap: _notifier.setMode,
                ),
                if (state.mode == 'timed')
                  TextField(
                    key: const Key('time-limit-input'),
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Thời gian (giây)',
                    ),
                    onChanged: (value) =>
                        _notifier.setTimeLimit(int.tryParse(value)),
                  ),
                if (state.mode == 'elimination')
                  TextField(
                    key: const Key('lives-input'),
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Số mạng'),
                    onChanged: (value) =>
                        _notifier.setLives(int.tryParse(value)),
                  ),
              ],
            ),
          ),
          _Section(
            title: 'Loại câu hỏi',
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _choice(
                  label: 'Word → Meaning',
                  selected: state.questionType == 1,
                  onSelected: () => _notifier.setQuestionType(1),
                ),
                _choice(
                  label: 'Meaning → Word',
                  selected: state.questionType == 2,
                  onSelected: () => _notifier.setQuestionType(2),
                ),
                _choice(
                  label: 'Description',
                  selected: state.questionType == 3,
                  onSelected: () => _notifier.setQuestionType(3),
                ),
              ],
            ),
          ),
          _Section(
            title: 'Cách trả lời',
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _choice(
                  label: 'Multiple Choice',
                  selected: state.answerMethod == 'multiple_choice',
                  onSelected: () =>
                      _notifier.setAnswerMethod('multiple_choice'),
                ),
                _choice(
                  label: 'Exact Typing',
                  selected: state.answerMethod == 'exact_typing',
                  onSelected: () => _notifier.setAnswerMethod('exact_typing'),
                ),
                _choice(
                  label: 'AI Typing',
                  selected: state.answerMethod == 'ai_typing',
                  onSelected: () => _notifier.setAnswerMethod('ai_typing'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          Tooltip(
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
            ),
          ),
        ],
      ),
    );
  }

  QuizConfigNotifier get _notifier => ref.read(quizConfigProvider.notifier);

  Widget _choice({
    required String label,
    required bool selected,
    required VoidCallback onSelected,
  }) {
    return ChoiceChip(
      label: Text(label),
      selected: selected,
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
                  name: topic.displayName,
                  wordCount: topic.wordCount,
                  icon: Icons.auto_stories_outlined,
                  emoji: topic.icon,
                ),
              )
              .toList(growable: false);

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
                  label: 'Danh sách của tôi',
                  icon: Icons.bookmark_outline,
                  selected: showingLists,
                  onTap: () => onTypeChanged(QuizSourceType.myList),
                ),
              ),
              Expanded(
                child: _SourceTypeButton(
                  key: const Key('quiz-source-personal-topic'),
                  label: 'Chủ đề cá nhân',
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
              ? 'Chọn một danh sách từ của bạn.'
              : 'Chọn một chủ đề chứa các từ bạn đã lưu.',
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
    this.emoji,
  });

  final int? id;
  final String name;
  final int wordCount;
  final IconData icon;
  final String? emoji;
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
                child: item.emoji?.trim().isNotEmpty == true
                    ? Text(item.emoji!, style: const TextStyle(fontSize: 19))
                    : Icon(item.icon, size: 18, color: AppColors.primary),
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
                          ? 'Chưa có từ để kiểm tra'
                          : '${item.wordCount} từ',
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
            ? 'Hãy lưu từ vào một chủ đề cá nhân trước khi kiểm tra.'
            : 'Hãy tạo danh sách và thêm từ trước khi kiểm tra.',
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
    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            child,
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
      icon: const Icon(Icons.calendar_month),
      label: Text(
        date == null
            ? label
            : '$label: ${DateFormat('dd/MM/yyyy').format(date!)}',
      ),
    );
  }
}

class _ModeTile extends StatelessWidget {
  const _ModeTile({
    required this.value,
    required this.title,
    required this.description,
    required this.selected,
    required this.onTap,
  });

  final String value;
  final String title;
  final String description;
  final bool selected;
  final ValueChanged<String> onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: selected
          ? Theme.of(context).colorScheme.primary.withValues(alpha: 0.18)
          : null,
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        key: Key('quiz-mode-$value'),
        onTap: () => onTap(value),
        leading: Icon(
          selected ? Icons.radio_button_checked : Icons.radio_button_off,
        ),
        title: Text(title),
        subtitle: Text(description),
      ),
    );
  }
}
