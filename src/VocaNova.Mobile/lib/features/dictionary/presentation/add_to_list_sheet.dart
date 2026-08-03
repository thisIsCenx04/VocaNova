import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_icon.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

enum _SaveDestination { lists, topics }

class AddToListSheet extends ConsumerStatefulWidget {
  const AddToListSheet({required this.wordId, this.eligibleTopics, super.key});

  final int wordId;
  final List<WordTopic>? eligibleTopics;

  @override
  ConsumerState<AddToListSheet> createState() => _AddToListSheetState();
}

class _AddToListSheetState extends ConsumerState<AddToListSheet> {
  final _noteController = TextEditingController();
  List<UserListSummary> _lists = const [];
  List<PersonalTopicSummary> _topics = const [];
  _SaveDestination _destination = _SaveDestination.lists;
  int? _selectedListId;
  int? _selectedTopicId;
  bool _isLoading = true;
  bool _isSaving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadDestinations();
  }

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);
    final height = math.min(620.0, media.size.height - media.viewInsets.bottom);
    return AnimatedPadding(
      duration: const Duration(milliseconds: 180),
      padding: EdgeInsets.only(bottom: media.viewInsets.bottom),
      child: Container(
        key: const Key('add-to-list-sheet'),
        height: height,
        padding: const EdgeInsets.fromLTRB(24, 13, 24, 16),
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Container(
                width: 36,
                height: 4,
                decoration: BoxDecoration(
                  color: const Color(0xFFEFEFEF),
                  borderRadius: BorderRadius.circular(99),
                ),
              ),
            ),
            const SizedBox(height: 18),
            Text(
              AppLocalizations.of(context)!.dictAddToListTooltip,
              style: AppTextStyles.button.copyWith(
                color: const Color(0xFF111111),
                fontSize: 18,
                height: 27 / 18,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              AppLocalizations.of(context)!.dictAddToListSheetSubtitle,
              style: AppTextStyles.caption.copyWith(
                color: const Color(0xFF77727F),
                fontSize: 13,
              ),
            ),
            const SizedBox(height: 14),
            SizedBox(
              width: double.infinity,
              child: SegmentedButton<_SaveDestination>(
                segments: [
                  ButtonSegment(
                    value: _SaveDestination.lists,
                    icon: const Icon(Icons.bookmark_outline, size: 18),
                    label: Text(AppLocalizations.of(context)!.dictMyListsLabel),
                  ),
                  ButtonSegment(
                    value: _SaveDestination.topics,
                    icon: const Icon(Icons.auto_stories_outlined, size: 18),
                    label: Text(
                      AppLocalizations.of(context)!.dictMyTopicsLabel,
                    ),
                  ),
                ],
                selected: {_destination},
                onSelectionChanged: _isSaving
                    ? null
                    : (selection) =>
                          setState(() => _destination = selection.first),
                style: ButtonStyle(
                  visualDensity: VisualDensity.compact,
                  backgroundColor: WidgetStateProperty.resolveWith(
                    (states) => states.contains(WidgetState.selected)
                        ? const Color(0xFFEDE7FF)
                        : Colors.white,
                  ),
                  foregroundColor: WidgetStateProperty.resolveWith(
                    (states) => states.contains(WidgetState.selected)
                        ? AppColors.primary
                        : const Color(0xFF5F5968),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 10),
            Expanded(child: _destinationContent()),
            if (_destination == _SaveDestination.lists)
              TextButton.icon(
                key: const Key('new-list-from-word'),
                onPressed: _isSaving ? null : _createList,
                style: TextButton.styleFrom(
                  foregroundColor: AppColors.primary,
                  padding: EdgeInsets.zero,
                  minimumSize: const Size(80, 40),
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
                icon: const Icon(Icons.add, size: 16),
                label: Text(AppLocalizations.of(context)!.dictNewListLabel),
              ),
            const SizedBox(height: 8),
            Text(
              AppLocalizations.of(context)!.dictNoteOptionalLabel,
              style: AppTextStyles.caption.copyWith(
                color: const Color(0xFF9E9E9E),
                fontSize: 13,
              ),
            ),
            const SizedBox(height: 6),
            SizedBox(
              height: 43,
              child: TextField(
                key: const Key('add-to-list-note'),
                controller: _noteController,
                maxLength: 500,
                buildCounter:
                    (
                      _, {
                      required currentLength,
                      required isFocused,
                      maxLength,
                    }) => null,
                decoration: InputDecoration(
                  hintText: AppLocalizations.of(context)!.dictAddNoteHint,
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 10,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 15),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: FilledButton(
                key: const Key('save-add-to-list'),
                onPressed: _canSave && !_isSaving ? _save : null,
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  disabledBackgroundColor: AppColors.primary.withValues(
                    alpha: 0.45,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
                child: _isSaving
                    ? const SizedBox.square(
                        dimension: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : Text(
                        _destination == _SaveDestination.lists
                            ? AppLocalizations.of(context)!.dictSaveToListLabel
                            : AppLocalizations.of(context)!.dictAddToTopicLabel,
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  bool get _canSave => switch (_destination) {
    _SaveDestination.lists => _selectedListId != null,
    _SaveDestination.topics => _selectedTopicId != null,
  };

  Widget _destinationContent() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(
        child: Text(_error!, style: const TextStyle(color: AppColors.error)),
      );
    }
    return switch (_destination) {
      _SaveDestination.lists => _listContent(),
      _SaveDestination.topics => _topicContent(),
    };
  }

  Widget _listContent() {
    if (_lists.isEmpty) {
      return Center(
        child: Text(AppLocalizations.of(context)!.dictCreateListPrompt),
      );
    }
    return ListView.builder(
      padding: EdgeInsets.zero,
      itemCount: _lists.length,
      itemBuilder: (context, index) {
        final list = _lists[index];
        final selected = list.listId == _selectedListId;
        return _DestinationRow(
          key: Key('add-to-list-${list.listId}'),
          icon: selected ? Icons.bookmark : Icons.bookmark_border,
          title: list.listName,
          subtitle: AppLocalizations.of(
            context,
          )!.dictWordCountLabel(list.wordCount),
          selected: selected,
          alternate: index.isOdd,
          onTap: _isSaving
              ? null
              : () => setState(() => _selectedListId = list.listId),
        );
      },
    );
  }

  Widget _topicContent() {
    if (_topics.isEmpty) {
      return Center(
        child: Text(AppLocalizations.of(context)!.dictNoSystemTopicAssignment),
      );
    }
    return ListView.builder(
      padding: EdgeInsets.zero,
      itemCount: _topics.length,
      itemBuilder: (context, index) {
        final topic = _topics[index];
        final selected = topic.topicId == _selectedTopicId;
        return _DestinationRow(
          key: Key('add-to-topic-${topic.topicId}'),
          icon: topic.containsWord
              ? Icons.check_circle
              : topicIconData(topic.icon, topic.name),
          title: topic.localizedName(context),
          subtitle: topic.containsWord
              ? AppLocalizations.of(context)!.dictAlreadyInTopic
              : AppLocalizations.of(
                  context,
                )!.dictPersonalWordCount(topic.wordCount),
          selected: selected,
          disabled: topic.containsWord,
          alternate: index.isOdd,
          onTap: _isSaving || topic.containsWord
              ? null
              : () => setState(() => _selectedTopicId = topic.topicId),
        );
      },
    );
  }

  Future<void> _loadDestinations() async {
    try {
      final repository = ref.read(wordDetailRepositoryProvider);
      final listsFuture = repository.getLists();
      final topicsFuture = repository.getPersonalTopics(wordId: widget.wordId);
      final wordFuture = widget.eligibleTopics == null
          ? repository.getWord(widget.wordId)
          : null;
      final lists = await listsFuture;
      final allPersonalTopics = await topicsFuture;
      final eligibleTopics =
          widget.eligibleTopics ?? (await wordFuture!).topics;
      final eligibleIds = eligibleTopics.map((topic) => topic.topicId).toSet();
      final topics = allPersonalTopics
          .where((topic) => eligibleIds.contains(topic.topicId))
          .toList(growable: false);
      if (!mounted) return;
      setState(() {
        _lists = lists;
        _topics = topics;
        _selectedListId = lists.isEmpty ? null : lists.first.listId;
        _selectedTopicId = topics
            .where((topic) => !topic.containsWord)
            .firstOrNull
            ?.topicId;
        _isLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = AppLocalizations.of(context)!.dictUnableToLoadDestinations;
        _isLoading = false;
      });
    }
  }

  Future<void> _createList() async {
    var pendingName = '';
    final name = await showDialog<String>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(AppLocalizations.of(context)!.dictNewListLabel),
        content: TextField(
          key: const Key('new-list-name'),
          autofocus: true,
          maxLength: 100,
          onChanged: (value) => pendingName = value,
          decoration: InputDecoration(
            hintText: AppLocalizations.of(context)!.dictListNameHint,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: Text(AppLocalizations.of(context)!.dictCancelLabel),
          ),
          FilledButton(
            key: const Key('create-list-from-word'),
            onPressed: () {
              final value = pendingName.trim();
              if (value.isNotEmpty) Navigator.pop(dialogContext, value);
            },
            child: Text(AppLocalizations.of(context)!.dictCreateLabel),
          ),
        ],
      ),
    );
    if (name == null || !mounted) return;

    setState(() => _isSaving = true);
    try {
      final list = await ref
          .read(wordDetailRepositoryProvider)
          .createList(name);
      if (!mounted) return;
      setState(() {
        _lists = [..._lists, list];
        _selectedListId = list.listId;
        _isSaving = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _isSaving = false);
      _showError(AppLocalizations.of(context)!.dictUnableToCreateList);
    }
  }

  Future<void> _save() async {
    setState(() => _isSaving = true);
    final repository = ref.read(wordDetailRepositoryProvider);
    final note = _noteController.text.trim();
    try {
      final String Function() destinationNameOf;
      if (_destination == _SaveDestination.lists) {
        final listId = _selectedListId!;
        await repository.addWordToList(
          listId: listId,
          wordId: widget.wordId,
          note: note,
        );
        destinationNameOf = () =>
            _lists.firstWhere((item) => item.listId == listId).listName;
      } else {
        final topicId = _selectedTopicId!;
        await repository.addWordToPersonalTopic(
          topicId: topicId,
          wordId: widget.wordId,
          note: note,
        );
        destinationNameOf = () => _topics
            .firstWhere((item) => item.topicId == topicId)
            .localizedName(context);
      }
      if (!mounted) return;
      final destinationName = destinationNameOf();
      final l10n = AppLocalizations.of(context)!;
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.dictAddedToDestination(destinationName))),
      );
    } catch (_) {
      if (!mounted) return;
      setState(() => _isSaving = false);
      _showError(AppLocalizations.of(context)!.dictUnableToSaveWord);
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

class _DestinationRow extends StatelessWidget {
  const _DestinationRow({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.selected,
    required this.onTap,
    this.disabled = false,
    this.alternate = false,
    super.key,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final bool selected;
  final bool disabled;
  final VoidCallback? onTap;

  /// Dòng lẻ đổi nền nhạt hơn để hai dòng liền nhau dễ phân biệt khi lướt.
  final bool alternate;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected
          ? AppColors.primary.withValues(alpha: 0.05)
          : alternate
          ? Color.alphaBlend(Colors.black.withValues(alpha: 0.05), Colors.white)
          : Colors.white,
      child: InkWell(
        onTap: onTap,
        child: SizedBox(
          height: 64,
          child: Row(
            children: [
              Container(
                width: 34,
                height: 34,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: disabled
                      ? const Color(0xFFE9F7EF)
                      : const Color(0xFFEDE7FF),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(
                  icon,
                  color: disabled ? const Color(0xFF2E9B62) : AppColors.primary,
                  size: 17,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.label.copyWith(
                        color: const Color(0xFF111111),
                        fontSize: 14,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      style: AppTextStyles.caption.copyWith(
                        color: disabled
                            ? const Color(0xFF2E8158)
                            : const Color(0xFF8A8490),
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
              if (selected)
                const Icon(
                  Icons.radio_button_checked,
                  color: AppColors.primary,
                ),
            ],
          ),
        ),
      ),
    );
  }
}
