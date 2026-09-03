import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_display_name.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_icon.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/models/user_list.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class ListsScreen extends ConsumerStatefulWidget {
  const ListsScreen({super.key});

  @override
  ConsumerState<ListsScreen> createState() => _ListsScreenState();
}

class _ListsScreenState extends ConsumerState<ListsScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(listsProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(listsProvider);
    final l10n = AppLocalizations.of(context)!;
    ref.listen(listsProvider.select((value) => value.errorMessage), (
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
      appBar: AppBar(title: Text(l10n.listsTitle)),
      floatingActionButton: FloatingActionButton(
        key: const Key('create-list-fab'),
        tooltip: l10n.listsCreateDialogTitle,
        onPressed: state.isMutating || state.isOffline
            ? null
            : () => _showNameDialog(),
        child: state.isMutating
            ? const SizedBox.square(
                dimension: 22,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : const Icon(Icons.add),
      ),
      body: Column(
        children: [
          if (state.isOffline) const _OfflineBanner(),
          Expanded(
            child: state.isLoading
                ? const Center(child: CircularProgressIndicator())
                : state.lists.isEmpty && state.personalTopics.isEmpty
                ? const _EmptyLists()
                : RefreshIndicator(
                    onRefresh: ref.read(listsProvider.notifier).load,
                    child: CustomScrollView(
                      key: const Key('lists-grid'),
                      physics: const AlwaysScrollableScrollPhysics(),
                      slivers: [
                        if (state.lists.isNotEmpty) ...[
                          SliverPadding(
                            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
                            sliver: SliverToBoxAdapter(
                              child: _ListSectionTitle(
                                key: const Key('user-lists-section'),
                                title: l10n.listsMyListsSection,
                              ),
                            ),
                          ),
                          SliverPadding(
                            padding: const EdgeInsets.symmetric(horizontal: 16),
                            sliver: SliverGrid.builder(
                              gridDelegate: _gridDelegate,
                              itemCount: state.lists.length,
                              itemBuilder: (_, index) {
                                final list = state.lists[index];
                                return _ListCard(
                                  list: list,
                                  onTap: () => context.push(
                                    AppRoutes.listDetail(
                                      list.listId.toString(),
                                    ),
                                  ),
                                  onLongPress: state.isOffline
                                      ? null
                                      : () => _showActions(list),
                                );
                              },
                            ),
                          ),
                        ],
                        if (state.personalTopics.isNotEmpty) ...[
                          SliverPadding(
                            padding: const EdgeInsets.fromLTRB(16, 20, 16, 8),
                            sliver: SliverToBoxAdapter(
                              child: _ListSectionTitle(
                                key: const Key('personal-topics-section'),
                                title: l10n.listsPersonalTopicsSection,
                              ),
                            ),
                          ),
                          SliverPadding(
                            padding: const EdgeInsets.symmetric(horizontal: 16),
                            sliver: SliverGrid.builder(
                              gridDelegate: _gridDelegate,
                              itemCount: state.personalTopics.length,
                              itemBuilder: (_, index) {
                                final topic = state.personalTopics[index];
                                return _PersonalTopicCard(
                                  topic: topic,
                                  onTap: () => context.push(
                                    AppRoutes.topicDetail(
                                      topic.topicId.toString(),
                                      personal: true,
                                    ),
                                    extra: topic,
                                  ),
                                );
                              },
                            ),
                          ),
                        ],
                        const SliverPadding(
                          padding: EdgeInsets.only(bottom: 96),
                        ),
                      ],
                    ),
                  ),
          ),
        ],
      ),
    );
  }

  static const _gridDelegate = SliverGridDelegateWithMaxCrossAxisExtent(
    maxCrossAxisExtent: 280,
    mainAxisExtent: 170,
    crossAxisSpacing: 12,
    mainAxisSpacing: 12,
  );

  Future<void> _showNameDialog({UserList? list}) async {
    final controller = TextEditingController(text: list?.listName);
    final formKey = GlobalKey<FormState>();
    final l10n = AppLocalizations.of(context)!;
    final name = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(
          list == null
              ? l10n.listsCreateDialogTitle
              : l10n.listsRenameDialogTitle,
        ),
        content: Form(
          key: formKey,
          child: TextFormField(
            key: const Key('list-name-field'),
            controller: controller,
            autofocus: true,
            maxLength: 100,
            decoration: InputDecoration(hintText: l10n.listsNameFieldHint),
            validator: _validateName,
            onFieldSubmitted: (_) {
              if (formKey.currentState!.validate()) {
                Navigator.pop(context, controller.text.trim());
              }
            },
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: Text(l10n.listsCancel),
          ),
          FilledButton(
            key: const Key('submit-list-name'),
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(context, controller.text.trim());
              }
            },
            child: Text(
              list == null ? l10n.listsCreateAction : l10n.listsSaveAction,
            ),
          ),
        ],
      ),
    );
    if (name == null || !mounted) return;
    if (list == null) {
      await ref.read(listsProvider.notifier).create(name);
    } else {
      await ref
          .read(listsProvider.notifier)
          .rename(listId: list.listId, name: name);
    }
  }

  Future<void> _showActions(UserList list) async {
    final l10n = AppLocalizations.of(context)!;
    final action = await showModalBottomSheet<_ListAction>(
      context: context,
      builder: (context) => SafeArea(
        child: Wrap(
          children: [
            ListTile(
              key: const Key('rename-list-action'),
              leading: const Icon(Icons.edit_outlined),
              title: Text(l10n.listsRenameAction),
              onTap: () => Navigator.pop(context, _ListAction.rename),
            ),
            ListTile(
              key: const Key('delete-list-action'),
              leading: Icon(Icons.delete_outline, color: AppColors.error),
              title: Text(l10n.listsDeleteAction),
              onTap: () => Navigator.pop(context, _ListAction.delete),
            ),
          ],
        ),
      ),
    );
    if (!mounted) return;
    if (action == _ListAction.rename) {
      await _showNameDialog(list: list);
    } else if (action == _ListAction.delete) {
      await _confirmDelete(list);
    }
  }

  Future<void> _confirmDelete(UserList list) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.listsDeleteConfirmTitle),
        content: Text(l10n.listsDeleteConfirmBody(list.listName)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.listsCancel),
          ),
          FilledButton(
            key: const Key('confirm-delete-list'),
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.listsDeleteAction),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(listsProvider.notifier).delete(list.listId);
    }
  }

  String? _validateName(String? value) {
    final l10n = AppLocalizations.of(context)!;
    final name = value?.trim() ?? '';
    if (name.isEmpty) return l10n.listsNameRequiredError;
    if (name.length > 100) return l10n.listsNameMaxLengthError;
    return null;
  }
}

enum _ListAction { rename, delete }

class _ListCard extends StatelessWidget {
  const _ListCard({
    required this.list,
    required this.onTap,
    required this.onLongPress,
  });

  final UserList list;
  final VoidCallback onTap;
  final VoidCallback? onLongPress;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Card(
      child: InkWell(
        key: Key('list-card-${list.listId}'),
        borderRadius: BorderRadius.circular(20),
        onTap: onTap,
        onLongPress: onLongPress,
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.collections_bookmark_outlined,
                color: Theme.of(context).colorScheme.primary,
                size: 32,
              ),
              const Spacer(),
              Text(
                list.listName,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 6),
              Text(l10n.listsWordCount(list.wordCount)),
              Text(
                l10n.listsCreatedOnLabel(
                  DateFormat('dd/MM/yyyy').format(list.createdAt),
                ),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PersonalTopicCard extends StatelessWidget {
  const _PersonalTopicCard({required this.topic, required this.onTap});

  final PersonalTopicSummary topic;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Card(
      child: InkWell(
        key: Key('personal-topic-card-${topic.topicId}'),
        borderRadius: BorderRadius.circular(20),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              TopicIcon(
                icon: topic.icon,
                name: topic.name,
                size: 32,
                color: Theme.of(context).colorScheme.primary,
              ),
              const Spacer(),
              Text(
                topic.localizedName(context),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 6),
              Text(l10n.listsWordCount(topic.wordCount)),
              Text(
                l10n.listsPersonalTopicsSection,
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ListSectionTitle extends StatelessWidget {
  const _ListSectionTitle({required this.title, super.key});

  final String title;

  @override
  Widget build(BuildContext context) => Text(
    title,
    style: Theme.of(
      context,
    ).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w800),
  );
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Container(
      key: const Key('lists-offline-banner'),
      width: double.infinity,
      color: AppColors.error.withValues(alpha: 0.18),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Text(l10n.listsOfflineBanner, textAlign: TextAlign.center),
    );
  }
}

class _EmptyLists extends StatelessWidget {
  const _EmptyLists();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Text(l10n.listsEmptyState, textAlign: TextAlign.center),
      ),
    );
  }
}
