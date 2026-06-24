import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';

class AddToListSheet extends ConsumerStatefulWidget {
  const AddToListSheet({required this.wordId, super.key});

  final int wordId;

  @override
  ConsumerState<AddToListSheet> createState() => _AddToListSheetState();
}

class _AddToListSheetState extends ConsumerState<AddToListSheet> {
  final _noteController = TextEditingController();
  List<UserListSummary> _lists = const [];
  int? _selectedListId;
  bool _isLoading = true;
  bool _isSaving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadLists();
  }

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);
    final height = math.min(536.0, media.size.height - media.viewInsets.bottom);
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
              'Add to list',
              style: AppTextStyles.button.copyWith(
                color: const Color(0xFF111111),
                fontSize: 18,
                height: 27 / 18,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 14),
            Expanded(child: _listContent()),
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
              label: const Text('New list'),
            ),
            const SizedBox(height: 10),
            Text(
              'Note (optional)',
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
                decoration: const InputDecoration(
                  hintText: 'Add a note...',
                  contentPadding: EdgeInsets.symmetric(
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
                onPressed: _selectedListId == null || _isSaving ? null : _save,
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
                    : const Text('Save'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _listContent() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(
        child: Text(_error!, style: const TextStyle(color: AppColors.error)),
      );
    }
    if (_lists.isEmpty) {
      return const Center(child: Text('Create a list to save this word.'));
    }
    return ListView.separated(
      padding: EdgeInsets.zero,
      itemCount: _lists.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final list = _lists[index];
        final selected = list.listId == _selectedListId;
        return Material(
          color: selected
              ? AppColors.primary.withValues(alpha: 0.04)
              : Colors.transparent,
          child: InkWell(
            key: Key('add-to-list-${list.listId}'),
            onTap: _isSaving
                ? null
                : () => setState(() => _selectedListId = list.listId),
            child: SizedBox(
              height: 66,
              child: Row(
                children: [
                  Container(
                    width: 32,
                    height: 32,
                    decoration: BoxDecoration(
                      color: const Color(0xFFEDE7FF),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(
                      selected ? Icons.bookmark : Icons.bookmark_border,
                      color: AppColors.primary,
                      size: 15,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      list.listName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.label.copyWith(
                        color: const Color(0xFF111111),
                        fontSize: 14,
                        height: 21 / 14,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Future<void> _loadLists() async {
    try {
      final lists = await ref.read(wordDetailRepositoryProvider).getLists();
      if (!mounted) return;
      setState(() {
        _lists = lists;
        _selectedListId = lists.isEmpty ? null : lists.first.listId;
        _isLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Unable to load your lists.';
        _isLoading = false;
      });
    }
  }

  Future<void> _createList() async {
    var pendingName = '';
    final name = await showDialog<String>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('New list'),
        content: TextField(
          key: const Key('new-list-name'),
          autofocus: true,
          maxLength: 100,
          onChanged: (value) => pendingName = value,
          decoration: const InputDecoration(hintText: 'List name'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancel'),
          ),
          FilledButton(
            key: const Key('create-list-from-word'),
            onPressed: () {
              final value = pendingName.trim();
              if (value.isNotEmpty) Navigator.pop(dialogContext, value);
            },
            child: const Text('Create'),
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
      _showError('Unable to create the list.');
    }
  }

  Future<void> _save() async {
    final listId = _selectedListId;
    if (listId == null) return;
    setState(() => _isSaving = true);
    try {
      await ref
          .read(wordDetailRepositoryProvider)
          .addWordToList(
            listId: listId,
            wordId: widget.wordId,
            note: _noteController.text.trim(),
          );
      if (!mounted) return;
      final listName = _lists
          .firstWhere((item) => item.listId == listId)
          .listName;
      Navigator.pop(context);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Added to $listName.')));
    } catch (_) {
      if (!mounted) return;
      setState(() => _isSaving = false);
      _showError('Unable to add this word.');
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}
