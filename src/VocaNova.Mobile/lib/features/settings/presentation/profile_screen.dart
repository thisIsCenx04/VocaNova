import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  @override
  void initState() {
    super.initState();
    if (ref.read(authProvider).user == null) {
      Future.microtask(ref.read(authProvider.notifier).loadCurrentUser);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authProvider);
    final user = auth.user;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Hồ sơ'),
        actions: [
          IconButton(
            tooltip: 'Cài đặt',
            onPressed: () => context.push(AppRoutes.settings),
            icon: const Icon(Icons.settings_outlined),
          ),
        ],
      ),
      body: user == null
          ? auth.status == AuthStatus.loading
                ? const Center(child: CircularProgressIndicator())
                : _ProfileError(
                    onRetry: ref.read(authProvider.notifier).loadCurrentUser,
                  )
          : _ProfileContent(
              user: user,
              isLoading: auth.status == AuthStatus.loading,
            ),
    );
  }
}

class _ProfileContent extends ConsumerWidget {
  const _ProfileContent({required this.user, required this.isLoading});

  final UserProfile user;
  final bool isLoading;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Center(
          child: CircleAvatar(
            key: const Key('profile-avatar'),
            radius: 48,
            backgroundImage: user.avatarUrl == null
                ? null
                : CachedNetworkImageProvider(user.avatarUrl!),
            child: user.avatarUrl == null
                ? Text(
                    user.displayName.isEmpty
                        ? '?'
                        : user.displayName[0].toUpperCase(),
                    style: Theme.of(context).textTheme.headlineLarge,
                  )
                : null,
          ),
        ),
        const SizedBox(height: 20),
        Card(
          child: Column(
            children: [
              ListTile(
                leading: const Icon(Icons.badge_outlined),
                title: const Text('Tên hiển thị'),
                subtitle: Text(user.displayName),
                trailing: const Icon(Icons.edit_outlined),
                onTap: isLoading
                    ? null
                    : () => _editDisplayName(context, ref, user),
              ),
              const Divider(height: 1),
              ListTile(
                leading: const Icon(Icons.phone_outlined),
                title: const Text('Số điện thoại'),
                subtitle: Text(maskPhone(user.phone)),
              ),
              const Divider(height: 1),
              ListTile(
                key: const Key('edit-learning-profile'),
                leading: const Icon(Icons.school_outlined),
                title: const Text('Hồ sơ học tập'),
                subtitle: const Text('Cập nhật mục tiêu và thông tin học tập'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => context.push(AppRoutes.onboarding),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        OutlinedButton.icon(
          key: const Key('logout-button'),
          onPressed: isLoading ? null : () => _confirmLogout(context, ref),
          icon: const Icon(Icons.logout),
          label: const Text('Đăng xuất'),
        ),
      ],
    );
  }

  Future<void> _editDisplayName(
    BuildContext context,
    WidgetRef ref,
    UserProfile user,
  ) async {
    final name = await showDialog<String>(
      context: context,
      builder: (context) => _DisplayNameDialog(initialName: user.displayName),
    );
    if (name == null || !context.mounted) return;
    final success = await ref
        .read(authProvider.notifier)
        .updateProfile(displayName: name, avatarUrl: user.avatarUrl);
    if (!success && context.mounted) {
      final message =
          ref.read(authProvider).errorMessage ?? 'Không thể cập nhật hồ sơ.';
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  Future<void> _confirmLogout(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Đăng xuất?'),
        content: const Text('Bạn sẽ cần đăng nhập lại để tiếp tục học.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          FilledButton(
            key: const Key('confirm-logout'),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(authProvider.notifier).logout();
    }
  }
}

class _DisplayNameDialog extends StatefulWidget {
  const _DisplayNameDialog({required this.initialName});

  final String initialName;

  @override
  State<_DisplayNameDialog> createState() => _DisplayNameDialogState();
}

class _DisplayNameDialogState extends State<_DisplayNameDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.initialName);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Sửa tên hiển thị'),
      content: Form(
        key: _formKey,
        child: TextFormField(
          key: const Key('display-name-field'),
          controller: _controller,
          autofocus: true,
          maxLength: 150,
          decoration: const InputDecoration(labelText: 'Tên hiển thị'),
          validator: (value) {
            final length = value?.trim().length ?? 0;
            return length < 2 ? 'Tên phải có ít nhất 2 ký tự' : null;
          },
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Hủy'),
        ),
        FilledButton(
          key: const Key('save-display-name'),
          onPressed: () {
            if (_formKey.currentState!.validate()) {
              Navigator.pop(context, _controller.text.trim());
            }
          },
          child: const Text('Lưu'),
        ),
      ],
    );
  }
}

String maskPhone(String? phone) {
  if (phone == null || phone.isEmpty) return 'Chưa cập nhật';
  if (phone.length <= 4) return List.filled(phone.length, '*').join();
  return '${phone.substring(0, 3)}${List.filled(phone.length - 5, '*').join()}${phone.substring(phone.length - 2)}';
}

class _ProfileError extends StatelessWidget {
  const _ProfileError({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: FilledButton(onPressed: onRetry, child: const Text('Thử lại')),
    );
  }
}
