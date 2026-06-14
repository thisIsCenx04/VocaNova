import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';

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
      appBar: AppBar(title: const Text('Danh sách từ')),
      floatingActionButton: FloatingActionButton(
        key: const Key('create-list-fab'),
        tooltip: 'Tạo danh sách',
        onPressed: state.isMutating ? null : () => _showNameDialog(),
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
                : state.lists.isEmpty
                ? const _EmptyLists()
                : RefreshIndicator(
                    onRefresh: ref.read(listsProvider.notifier).load,
                    child: GridView.builder(
                      key: const Key('lists-grid'),
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 96),
                      gridDelegate:
                          const SliverGridDelegateWithMaxCrossAxisExtent(
                            maxCrossAxisExtent: 280,
                            mainAxisExtent: 170,
                            crossAxisSpacing: 12,
                            mainAxisSpacing: 12,
                          ),
                      itemCount: state.lists.length,
                      itemBuilder: (_, index) {
                        final list = state.lists[index];
                        return _ListCard(
                          list: list,
                          onTap: () => context.push(
                            AppRoutes.listDetail(list.listId.toString()),
                          ),
                          onLongPress: () => _showActions(list),
                        );
                      },
                    ),
                  ),
          ),
        ],
      ),
    );
  }

  Future<void> _showNameDialog({UserList? list}) async {
    final controller = TextEditingController(text: list?.listName);
    final formKey = GlobalKey<FormState>();
    final name = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(list == null ? 'Tạo danh sách' : 'Đổi tên danh sách'),
        content: Form(
          key: formKey,
          child: TextFormField(
            key: const Key('list-name-field'),
            controller: controller,
            autofocus: true,
            maxLength: 100,
            decoration: const InputDecoration(hintText: 'Tên danh sách'),
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
            child: const Text('Hủy'),
          ),
          FilledButton(
            key: const Key('submit-list-name'),
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(context, controller.text.trim());
              }
            },
            child: Text(list == null ? 'Tạo' : 'Lưu'),
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
    final action = await showModalBottomSheet<_ListAction>(
      context: context,
      builder: (context) => SafeArea(
        child: Wrap(
          children: [
            ListTile(
              key: const Key('rename-list-action'),
              leading: const Icon(Icons.edit_outlined),
              title: const Text('Đổi tên'),
              onTap: () => Navigator.pop(context, _ListAction.rename),
            ),
            ListTile(
              key: const Key('delete-list-action'),
              leading: Icon(Icons.delete_outline, color: AppColors.error),
              title: const Text('Xóa'),
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
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xóa danh sách?'),
        content: Text('Danh sách "${list.listName}" sẽ bị xóa.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          FilledButton(
            key: const Key('confirm-delete-list'),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(listsProvider.notifier).delete(list.listId);
    }
  }

  String? _validateName(String? value) {
    final name = value?.trim() ?? '';
    if (name.isEmpty) return 'Vui lòng nhập tên danh sách.';
    if (name.length > 100) return 'Tên danh sách tối đa 100 ký tự.';
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
  final VoidCallback onLongPress;

  @override
  Widget build(BuildContext context) {
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
              Text('${list.wordCount} từ'),
              Text(
                'Tạo ngày ${DateFormat('dd/MM/yyyy').format(list.createdAt)}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('lists-offline-banner'),
      width: double.infinity,
      color: AppColors.error.withValues(alpha: 0.18),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: const Text(
        'Bạn đang offline. Đang hiển thị danh sách đã lưu.',
        textAlign: TextAlign.center,
      ),
    );
  }
}

class _EmptyLists extends StatelessWidget {
  const _EmptyLists();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'Bạn chưa có danh sách từ nào.\nNhấn + để tạo danh sách đầu tiên.',
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}
