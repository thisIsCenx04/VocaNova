import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/app/theme/app_theme.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';

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
      backgroundColor: Colors.white,
      body: SafeArea(
        bottom: false,
        child: user == null
            ? auth.status == AuthStatus.loading
                  ? const Center(child: CircularProgressIndicator())
                  : _ProfileError(
                      onRetry: ref.read(authProvider.notifier).loadCurrentUser,
                    )
            : _ProfileContent(
                user: user,
                isLoading: auth.status == AuthStatus.loading,
              ),
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
      padding: EdgeInsets.zero,
      children: [
        _ProfileHeader(
          user: user,
          onEdit: isLoading
              ? null
              : () => _showEditProfileSheet(context, ref, user),
        ),
        const _SectionLabel('LEARNING'),
        _ProfileMenuGroup(
          children: [
            _ProfileMenuRow(
              icon: Icons.menu_book_outlined,
              title: 'My vocabulary',
              subtitle: '248 words collected',
              onTap: () => context.go(AppRoutes.lists),
            ),
            _ProfileMenuRow(
              icon: Icons.query_stats_outlined,
              title: 'Statistics',
              subtitle: 'Progress & analytics',
              onTap: () => context.push(AppRoutes.progressCharts),
            ),
            const _ProfileMenuRow(
              icon: Icons.history,
              title: 'Test history',
              subtitle: 'Past practice sessions',
            ),
            _ProfileMenuRow(
              key: const Key('edit-learning-profile'),
              icon: Icons.flag_outlined,
              title: 'Learning goals',
              subtitle: 'B2 \u2192 C1 target',
              onTap: () => context.push(AppRoutes.onboarding),
            ),
          ],
        ),
        const _SectionLabel('ACCOUNT'),
        _ProfileMenuGroup(
          children: [
            _ProfileMenuRow(
              key: const Key('edit-profile-button'),
              icon: Icons.person_outline,
              title: 'Personal information',
              subtitle: 'Name, avatar, phone',
              onTap: isLoading
                  ? null
                  : () => _showEditProfileSheet(context, ref, user),
            ),
            _ProfileMenuRow(
              key: const Key('change-password-button'),
              icon: Icons.lock_outline,
              title: 'Change password',
              subtitle: 'Keep your account secure',
              onTap: isLoading
                  ? null
                  : () => _showChangePasswordSheet(context, ref),
            ),
            _ProfileMenuRow(
              icon: Icons.notifications_none,
              title: 'Notifications',
              subtitle: 'Daily reminders on',
              trailing: const _StatusBadge(),
              onTap: () => context.push(AppRoutes.settings),
            ),
            _ProfileMenuRow(
              icon: Icons.translate,
              title: 'Language',
              subtitle: 'Vietnamese / English',
              onTap: () => context.push(AppRoutes.settings),
            ),
            _ProfileMenuRow(
              icon: Icons.light_mode_outlined,
              title: 'Theme',
              subtitle: 'System default',
              onTap: () => context.push(AppRoutes.settings),
            ),
          ],
        ),
        const _SectionLabel('APP', muted: true),
        _ProfileMenuGroup(
          children: [
            _ProfileMenuRow(
              icon: Icons.settings_outlined,
              title: 'Settings',
              subtitle: 'Audio, storage, sync',
              onTap: () => context.push(AppRoutes.settings),
            ),
            const _ProfileMenuRow(
              icon: Icons.shield_outlined,
              title: 'Privacy & data',
              subtitle: 'Manage your data',
            ),
            const _ProfileMenuRow(
              icon: Icons.help_outline,
              title: 'Help & feedback',
              subtitle: 'FAQs and support',
            ),
          ],
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
          child: TextButton.icon(
            key: const Key('logout-button'),
            onPressed: isLoading ? null : () => _confirmLogout(context, ref),
            style: TextButton.styleFrom(
              alignment: Alignment.centerLeft,
              foregroundColor: const Color(0xFFDC2626),
              minimumSize: const Size.fromHeight(48),
              padding: EdgeInsets.zero,
              shape: const RoundedRectangleBorder(),
            ),
            icon: const Icon(Icons.logout, size: 17),
            label: Text(
              'Sign out',
              style: AppTextStyles.label.copyWith(
                color: const Color(0xFFDC2626),
                fontSize: 14,
              ),
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 4, 20, 26),
          child: Text(
            'VocaNova v1.0.0 \u00b7 SEP490_19',
            style: AppTextStyles.caption.copyWith(
              color: const Color(0xFF9C99B5),
              fontSize: 12,
              height: 18 / 12,
            ),
          ),
        ),
      ],
    );
  }

  Future<void> _showEditProfileSheet(
    BuildContext context,
    WidgetRef ref,
    UserProfile user,
  ) async {
    final result = await showModalBottomSheet<_EditProfileResult>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (_) => _EditProfileSheet(user: user),
    );
    if (result == null || !context.mounted) return;

    var avatarUrl = result.avatarUrl;
    if (result.avatarBytes != null && result.avatarFileName != null) {
      final uploaded = await ref
          .read(authProvider.notifier)
          .uploadAvatar(
            bytes: result.avatarBytes!,
            fileName: result.avatarFileName!,
          );
      if (!context.mounted) return;
      if (!uploaded) {
        _showMessage(
          context,
          ref.read(authProvider).errorMessage ?? 'Unable to upload avatar.',
        );
        return;
      }
      avatarUrl = ref.read(authProvider).user?.avatarUrl;
    }

    final success = await ref
        .read(authProvider.notifier)
        .updateProfile(displayName: result.displayName, avatarUrl: avatarUrl);
    if (!context.mounted) return;
    if (success) {
      _showMessage(context, 'Profile updated successfully.');
    } else {
      _showMessage(
        context,
        ref.read(authProvider).errorMessage ?? 'Unable to update profile.',
      );
    }
  }

  Future<void> _showChangePasswordSheet(
    BuildContext context,
    WidgetRef ref,
  ) async {
    final result = await showModalBottomSheet<_ChangePasswordResult>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (_) => const _ChangePasswordSheet(),
    );
    if (result == null || !context.mounted) return;

    final success = await ref
        .read(authProvider.notifier)
        .changePassword(
          currentPassword: result.currentPassword,
          newPassword: result.newPassword,
        );
    if (!context.mounted) return;
    if (success) {
      _showMessage(context, 'Password changed successfully.');
    } else {
      _showMessage(
        context,
        ref.read(authProvider).errorMessage ?? 'Unable to change password.',
      );
    }
  }

  Future<void> _confirmLogout(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Sign out?'),
        content: const Text('You will need to sign in again to keep learning.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            key: const Key('confirm-logout'),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(authProvider.notifier).logout();
    }
  }
}

class _ProfileHeader extends StatelessWidget {
  const _ProfileHeader({required this.user, required this.onEdit});

  final UserProfile user;
  final VoidCallback? onEdit;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 21),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(
          bottom: BorderSide(color: Color(0xFFECEAF4), width: 1.25),
        ),
      ),
      child: Column(
        children: [
          InkWell(
            onTap: onEdit,
            child: Row(
              children: [
                _ProfileAvatar(user: user),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        user.displayName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: AppTextStyles.heading.copyWith(
                          color: const Color(0xFF14142B),
                          fontSize: 18,
                          height: 27 / 18,
                        ),
                      ),
                      Text(
                        user.phone == null
                            ? 'Phone not linked'
                            : maskPhone(user.phone),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: AppTextStyles.caption.copyWith(
                          color: const Color(0xFF444444),
                          fontSize: 13,
                          height: 19.5 / 13,
                        ),
                      ),
                      const SizedBox(height: 8),
                      const Row(
                        children: [
                          _ProfileChip(
                            label: 'B2 level',
                            background: Color(0xFFF1EEFF),
                            foreground: AppColors.primary,
                          ),
                          SizedBox(width: 8),
                          _ProfileChip(
                            label: '7-day streak',
                            icon: Icons.local_fire_department,
                            background: Color(0xFFFFF1D9),
                            foreground: Color(0xFFC2410C),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                TextButton(
                  key: const Key('profile-edit-action'),
                  onPressed: onEdit,
                  style: TextButton.styleFrom(
                    foregroundColor: const Color(0xFF9C99B5),
                    padding: const EdgeInsets.symmetric(horizontal: 4),
                    minimumSize: const Size(28, 36),
                  ),
                  child: Text(
                    'Edit',
                    style: AppTextStyles.label.copyWith(
                      color: const Color(0xFF9C99B5),
                      fontSize: 12,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          Container(
            padding: const EdgeInsets.only(top: 16),
            decoration: const BoxDecoration(
              border: Border(
                top: BorderSide(color: Color(0xFFECEAF4), width: 1.25),
              ),
            ),
            child: const Row(
              children: [
                _ProfileStat(value: '248', label: 'Words'),
                _ProfileStat(value: '84%', label: 'Accuracy'),
                _ProfileStat(value: '7', label: 'Streak'),
                _ProfileStat(value: '12', label: 'Badges', last: true),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ProfileAvatar extends StatelessWidget {
  const _ProfileAvatar({required this.user});

  final UserProfile user;

  @override
  Widget build(BuildContext context) {
    final initials = _initials(user.displayName);
    return Stack(
      clipBehavior: Clip.none,
      children: [
        Container(
          key: const Key('profile-avatar'),
          width: 64,
          height: 64,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: user.avatarUrl == null
                ? const LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [
                      Color(0xFF7A5BFF),
                      AppColors.primary,
                      Color(0xFF5B36FF),
                    ],
                  )
                : null,
            image: user.avatarUrl == null
                ? null
                : DecorationImage(
                    image: CachedNetworkImageProvider(user.avatarUrl!),
                    fit: BoxFit.cover,
                  ),
          ),
          alignment: Alignment.center,
          child: user.avatarUrl == null
              ? Text(
                  initials,
                  style: AppTextStyles.heading.copyWith(
                    color: Colors.white,
                    fontSize: 20,
                    height: 30 / 20,
                  ),
                )
              : null,
        ),
        Positioned(
          right: 1,
          bottom: 1,
          child: Container(
            width: 14,
            height: 14,
            decoration: BoxDecoration(
              color: const Color(0xFF16A34A),
              shape: BoxShape.circle,
              border: Border.all(color: Colors.white, width: 1.25),
            ),
          ),
        ),
      ],
    );
  }
}

class _ProfileChip extends StatelessWidget {
  const _ProfileChip({
    required this.label,
    required this.background,
    required this.foreground,
    this.icon,
  });

  final String label;
  final Color background;
  final Color foreground;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 24,
      padding: const EdgeInsets.symmetric(horizontal: 10),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 10, color: foreground),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: AppTextStyles.caption.copyWith(
              color: foreground,
              fontSize: 12,
              height: 16 / 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class _ProfileStat extends StatelessWidget {
  const _ProfileStat({
    required this.value,
    required this.label,
    this.last = false,
  });

  final String value;
  final String label;
  final bool last;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 4),
        decoration: BoxDecoration(
          border: last
              ? null
              : const Border(
                  right: BorderSide(color: Color(0xFFECEAF4), width: 1.25),
                ),
        ),
        child: Column(
          children: [
            Text(
              value,
              style: AppTextStyles.heading.copyWith(
                color: const Color(0xFF14142B),
                fontSize: 18,
                height: 27 / 18,
              ),
            ),
            Text(
              label,
              style: AppTextStyles.caption.copyWith(
                color: const Color(0xFF444444),
                fontSize: 11,
                height: 16.5 / 11,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.label, {this.muted = false});

  final String label;
  final bool muted;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
      child: Text(
        label,
        style: AppTextStyles.caption.copyWith(
          color: muted ? const Color(0xFF9C99B5) : const Color(0xFF444444),
          fontFamily: 'JetBrains Mono',
          fontSize: 11,
          height: 16.5 / 11,
          letterSpacing: 0.5,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _ProfileMenuGroup extends StatelessWidget {
  const _ProfileMenuGroup({required this.children});

  final List<_ProfileMenuRow> children;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border.symmetric(
          horizontal: BorderSide(color: Color(0xFFECEAF4), width: 1.25),
        ),
      ),
      child: Column(
        children: [
          for (var index = 0; index < children.length; index++)
            _MenuRowWithDivider(
              showDivider: index < children.length - 1,
              child: children[index],
            ),
        ],
      ),
    );
  }
}

class _MenuRowWithDivider extends StatelessWidget {
  const _MenuRowWithDivider({required this.child, required this.showDivider});

  final Widget child;
  final bool showDivider;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        border: showDivider
            ? const Border(
                bottom: BorderSide(color: Color(0xFFECEAF4), width: 1.25),
              )
            : null,
      ),
      child: child,
    );
  }
}

class _ProfileMenuRow extends StatelessWidget {
  const _ProfileMenuRow({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    this.trailing,
    this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: 68),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 14, 20, 14),
          child: Row(
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: const Color(0xFFF1EEFF),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(icon, size: 16, color: AppColors.primary),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.label.copyWith(
                        color: const Color(0xFF14142B),
                        fontSize: 14,
                        height: 21 / 14,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    Text(
                      subtitle,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.caption.copyWith(
                        color: const Color(0xFF444444),
                        fontSize: 12,
                        height: 18 / 12,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              trailing ??
                  const Icon(
                    Icons.chevron_right,
                    color: Color(0xFF9C99B5),
                    size: 17,
                  ),
            ],
          ),
        ),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(
        color: const Color(0xFFDCFCE7),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(
        'ON',
        style: AppTextStyles.caption.copyWith(
          color: const Color(0xFF16A34A),
          fontSize: 12,
          height: 16 / 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _EditProfileSheet extends StatefulWidget {
  const _EditProfileSheet({required this.user});

  final UserProfile user;

  @override
  State<_EditProfileSheet> createState() => _EditProfileSheetState();
}

class _EditProfileSheetState extends State<_EditProfileSheet> {
  static const _maxAvatarBytes = 5 * 1024 * 1024;
  static const _suggestedAvatars = [
    _SuggestedAvatar('Purple cat', 'assets/avatars/cat.png'),
    _SuggestedAvatar('Teal owl', 'assets/avatars/owl.png'),
    _SuggestedAvatar('Coral fox', 'assets/avatars/fox.png'),
    _SuggestedAvatar('Monstera', 'assets/avatars/monstera.png'),
    _SuggestedAvatar('Sunflower', 'assets/avatars/sunflower.png'),
    _SuggestedAvatar('Pine tree', 'assets/avatars/pine.png'),
  ];

  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _nameController;
  Uint8List? _avatarBytes;
  String? _avatarFileName;
  String? _selectedAsset;
  bool _isPicking = false;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.user.displayName);
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _ProfileFormSheet(
      title: 'Personal information',
      subtitle: 'Update your profile details.',
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _SheetFieldLabel(
              label: 'Profile picture',
              child: _AvatarPicker(
                user: widget.user,
                bytes: _avatarBytes,
                isPicking: _isPicking,
                onPick: _pickFromGallery,
              ),
            ),
            const SizedBox(height: 18),
            Text(
              'Choose an avatar',
              style: AppTextStyles.label.copyWith(
                color: const Color(0xFF444444),
                fontSize: 13,
              ),
            ),
            const SizedBox(height: 10),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                for (final avatar in _suggestedAvatars)
                  _SuggestedAvatarButton(
                    avatar: avatar,
                    selected: _selectedAsset == avatar.assetPath,
                    onTap: () => _selectSuggestedAvatar(avatar),
                  ),
              ],
            ),
            const SizedBox(height: 22),
            _SheetFieldLabel(
              label: 'Full name',
              child: TextFormField(
                key: const Key('display-name-field'),
                controller: _nameController,
                maxLength: 150,
                textInputAction: TextInputAction.next,
                decoration: const InputDecoration(
                  hintText: 'Nguyen Van An',
                  counterText: '',
                ),
                validator: (value) {
                  final length = value?.trim().length ?? 0;
                  if (length < 2) {
                    return 'Name must contain at least 2 characters';
                  }
                  return null;
                },
              ),
            ),
            const SizedBox(height: 16),
            _SheetFieldLabel(
              label: 'Phone number',
              child: TextFormField(
                initialValue: widget.user.phone ?? 'Not linked',
                enabled: false,
                decoration: const InputDecoration(),
              ),
            ),
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('save-display-name'),
              onPressed: _submit,
              child: const Text('Save changes'),
            ),
          ],
        ),
      ),
    );
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) return;
    Navigator.pop(
      context,
      _EditProfileResult(
        displayName: _nameController.text.trim(),
        avatarUrl: widget.user.avatarUrl,
        avatarBytes: _avatarBytes,
        avatarFileName: _avatarFileName,
      ),
    );
  }

  Future<void> _pickFromGallery() async {
    if (_isPicking) return;
    setState(() => _isPicking = true);
    try {
      final file = await ImagePicker().pickImage(
        source: ImageSource.gallery,
        maxWidth: 1200,
        maxHeight: 1200,
        imageQuality: 90,
      );
      if (file == null || !mounted) return;
      final bytes = await file.readAsBytes();
      if (!mounted) return;
      if (bytes.length > _maxAvatarBytes) {
        _showMessage(context, 'Avatar must be 5MB or smaller.');
        return;
      }
      setState(() {
        _avatarBytes = bytes;
        _avatarFileName = _normalizedFileName(file.name);
        _selectedAsset = null;
      });
    } catch (_) {
      if (mounted) {
        _showMessage(context, 'Unable to open the photo library.');
      }
    } finally {
      if (mounted) setState(() => _isPicking = false);
    }
  }

  Future<void> _selectSuggestedAvatar(_SuggestedAvatar avatar) async {
    final data = await rootBundle.load(avatar.assetPath);
    if (!mounted) return;
    setState(() {
      _avatarBytes = data.buffer.asUint8List(
        data.offsetInBytes,
        data.lengthInBytes,
      );
      _avatarFileName = avatar.assetPath.split('/').last;
      _selectedAsset = avatar.assetPath;
    });
  }

  String _normalizedFileName(String name) {
    final lower = name.toLowerCase();
    if (lower.endsWith('.jpg') ||
        lower.endsWith('.jpeg') ||
        lower.endsWith('.png') ||
        lower.endsWith('.webp')) {
      return name;
    }
    return 'avatar.jpg';
  }
}

class _ChangePasswordSheet extends StatefulWidget {
  const _ChangePasswordSheet();

  @override
  State<_ChangePasswordSheet> createState() => _ChangePasswordSheetState();
}

class _AvatarPicker extends StatelessWidget {
  const _AvatarPicker({
    required this.user,
    required this.bytes,
    required this.isPicking,
    required this.onPick,
  });

  final UserProfile user;
  final Uint8List? bytes;
  final bool isPicking;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    ImageProvider<Object>? image;
    if (bytes != null) {
      image = MemoryImage(bytes!);
    } else if (user.avatarUrl != null && user.avatarUrl!.isNotEmpty) {
      image = CachedNetworkImageProvider(user.avatarUrl!);
    }

    return Row(
      children: [
        Container(
          key: const Key('avatar-preview'),
          width: 72,
          height: 72,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: image == null
                ? const LinearGradient(
                    colors: [Color(0xFF6949FF), Color(0xFF3B238D)],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  )
                : null,
            image: image == null
                ? null
                : DecorationImage(image: image, fit: BoxFit.cover),
          ),
          alignment: Alignment.center,
          child: image == null
              ? Text(
                  _initials(user.displayName),
                  style: AppTextStyles.heading.copyWith(
                    color: Colors.white,
                    fontSize: 20,
                  ),
                )
              : null,
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              OutlinedButton.icon(
                key: const Key('pick-avatar-button'),
                onPressed: isPicking ? null : onPick,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size.fromHeight(44),
                  foregroundColor: const Color(0xFF14142B),
                  backgroundColor: Colors.white,
                  side: const BorderSide(color: Color(0xFFE5E5E5), width: 1.25),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
                icon: isPicking
                    ? const SizedBox.square(
                        dimension: 16,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.photo_library_outlined, size: 18),
                label: Text(isPicking ? 'Opening...' : 'Choose from device'),
              ),
              const SizedBox(height: 5),
              Text(
                'JPG, PNG or WebP · Max 5MB',
                style: AppTextStyles.caption.copyWith(
                  color: const Color(0xFF8A8A8A),
                  fontSize: 11,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _SuggestedAvatarButton extends StatelessWidget {
  const _SuggestedAvatarButton({
    required this.avatar,
    required this.selected,
    required this.onTap,
  });

  final _SuggestedAvatar avatar;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: avatar.label,
      child: InkWell(
        key: Key('suggested-avatar-${avatar.label}'),
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          width: 46,
          height: 46,
          padding: const EdgeInsets.all(2),
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: Border.all(
              color: selected
                  ? const Color(0xFF6949FF)
                  : const Color(0xFFE5E5E5),
              width: selected ? 2.5 : 1.25,
            ),
          ),
          child: ClipOval(
            child: Image.asset(avatar.assetPath, fit: BoxFit.cover),
          ),
        ),
      ),
    );
  }
}

class _ChangePasswordSheetState extends State<_ChangePasswordSheet> {
  final _formKey = GlobalKey<FormState>();
  final _currentController = TextEditingController();
  final _newController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _showCurrent = false;
  bool _showNew = false;
  bool _showConfirm = false;

  @override
  void dispose() {
    _currentController.dispose();
    _newController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _ProfileFormSheet(
      title: 'Change password',
      subtitle: 'Use at least 8 characters with upper, lower and a number.',
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _SheetFieldLabel(
              label: 'Current password',
              child: TextFormField(
                key: const Key('current-password-field'),
                controller: _currentController,
                obscureText: !_showCurrent,
                textInputAction: TextInputAction.next,
                decoration: InputDecoration(
                  hintText: 'Enter current password',
                  suffixIcon: _PasswordVisibilityButton(
                    visible: _showCurrent,
                    onPressed: () =>
                        setState(() => _showCurrent = !_showCurrent),
                  ),
                ),
                validator: AuthValidators.loginPassword,
              ),
            ),
            const SizedBox(height: 16),
            _SheetFieldLabel(
              label: 'New password',
              child: TextFormField(
                key: const Key('new-password-field'),
                controller: _newController,
                obscureText: !_showNew,
                textInputAction: TextInputAction.next,
                decoration: InputDecoration(
                  hintText: 'At least 8 characters',
                  suffixIcon: _PasswordVisibilityButton(
                    visible: _showNew,
                    onPressed: () => setState(() => _showNew = !_showNew),
                  ),
                ),
                validator: AuthValidators.strongPassword,
                onChanged: (_) {
                  if (_confirmController.text.isNotEmpty) {
                    _formKey.currentState?.validate();
                  }
                },
              ),
            ),
            const SizedBox(height: 16),
            _SheetFieldLabel(
              label: 'Confirm new password',
              child: TextFormField(
                key: const Key('confirm-new-password-field'),
                controller: _confirmController,
                obscureText: !_showConfirm,
                textInputAction: TextInputAction.done,
                decoration: InputDecoration(
                  hintText: 'Repeat your password',
                  suffixIcon: _PasswordVisibilityButton(
                    visible: _showConfirm,
                    onPressed: () =>
                        setState(() => _showConfirm = !_showConfirm),
                  ),
                ),
                validator: (value) =>
                    AuthValidators.confirmPassword(value, _newController.text),
                onFieldSubmitted: (_) => _submit(),
              ),
            ),
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('save-password-button'),
              onPressed: _submit,
              child: const Text('Update password'),
            ),
          ],
        ),
      ),
    );
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) return;
    Navigator.pop(
      context,
      _ChangePasswordResult(
        currentPassword: _currentController.text,
        newPassword: _newController.text,
      ),
    );
  }
}

class _ProfileFormSheet extends StatelessWidget {
  const _ProfileFormSheet({
    required this.title,
    required this.subtitle,
    required this.child,
  });

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Theme(
      data: AppTheme.light(),
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(
          24,
          12,
          24,
          24 + MediaQuery.viewInsetsOf(context).bottom,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: const Color(0xFFD9D7E3),
                  borderRadius: BorderRadius.circular(999),
                ),
              ),
            ),
            const SizedBox(height: 22),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: AppTextStyles.heading.copyWith(
                          color: const Color(0xFF14142B),
                          fontSize: 22,
                          height: 33 / 22,
                          letterSpacing: -0.3,
                        ),
                      ),
                      Text(
                        subtitle,
                        style: AppTextStyles.caption.copyWith(
                          color: const Color(0xFF444444),
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  tooltip: 'Close',
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.close, size: 20),
                ),
              ],
            ),
            const SizedBox(height: 24),
            child,
          ],
        ),
      ),
    );
  }
}

class _SheetFieldLabel extends StatelessWidget {
  const _SheetFieldLabel({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: AppTextStyles.label.copyWith(
            color: const Color(0xFF14142B),
            fontSize: 13,
          ),
        ),
        const SizedBox(height: 6),
        child,
      ],
    );
  }
}

class _PasswordVisibilityButton extends StatelessWidget {
  const _PasswordVisibilityButton({
    required this.visible,
    required this.onPressed,
  });

  final bool visible;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: visible ? 'Hide password' : 'Show password',
      onPressed: onPressed,
      icon: Icon(
        visible ? Icons.visibility_off_outlined : Icons.visibility_outlined,
        size: 18,
      ),
    );
  }
}

class _EditProfileResult {
  const _EditProfileResult({
    required this.displayName,
    required this.avatarUrl,
    required this.avatarBytes,
    required this.avatarFileName,
  });

  final String displayName;
  final String? avatarUrl;
  final Uint8List? avatarBytes;
  final String? avatarFileName;
}

class _SuggestedAvatar {
  const _SuggestedAvatar(this.label, this.assetPath);

  final String label;
  final String assetPath;
}

class _ChangePasswordResult {
  const _ChangePasswordResult({
    required this.currentPassword,
    required this.newPassword,
  });

  final String currentPassword;
  final String newPassword;
}

String maskPhone(String? phone) {
  if (phone == null || phone.isEmpty) return 'Chưa cập nhật';
  if (phone.length <= 4) return List.filled(phone.length, '*').join();
  return '${phone.substring(0, 3)}'
      '${List.filled(phone.length - 5, '*').join()}'
      '${phone.substring(phone.length - 2)}';
}

String _initials(String displayName) {
  final parts = displayName
      .trim()
      .split(RegExp(r'\s+'))
      .where((part) => part.isNotEmpty)
      .toList();
  if (parts.isEmpty) return '?';
  if (parts.length == 1) {
    return parts.first.substring(0, 1).toUpperCase();
  }
  return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
}

void _showMessage(BuildContext context, String message) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));
}

class _ProfileError extends StatelessWidget {
  const _ProfileError({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: FilledButton(onPressed: onRetry, child: const Text('Try again')),
    );
  }
}
