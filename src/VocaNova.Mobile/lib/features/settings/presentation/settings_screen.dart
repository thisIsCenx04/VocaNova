import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/models/auth_state.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

const appVersion = '1.0.0';

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final deletingAccount =
        ref.watch(authProvider).status == AuthStatus.loading;
    return ListenableBuilder(
      listenable: AppSettingsNotifier.instance,
      builder: (context, _) {
        final notifier = AppSettingsNotifier.instance;
        final settings = notifier.state;
        final palette = _SettingsPalette.of(context);
        final l10n = AppLocalizations.of(context)!;

        return Scaffold(
          backgroundColor: palette.background,
          body: SafeArea(
            bottom: false,
            child: Column(
              children: [
                _SettingsHeader(palette: palette),
                Expanded(
                  child: ListView(
                    key: const Key('settings-list'),
                    padding: EdgeInsets.zero,
                    children: [
                      _SectionLabel(
                        label: l10n.settingsSectionAppearance,
                        palette: palette,
                      ),
                      _SettingsRow(
                        icon: Icons.dark_mode_outlined,
                        title: l10n.settingsDarkMode,
                        subtitle: l10n.settingsDarkModeSubtitle,
                        palette: palette,
                        showDivider: true,
                        trailing: _VocaSwitch(
                          key: const Key('theme-switch'),
                          value: settings.themeMode == ThemeMode.dark,
                          onChanged: (enabled) => notifier.setThemeMode(
                            enabled ? ThemeMode.dark : ThemeMode.light,
                          ),
                        ),
                      ),
                      _SettingsRow(
                        icon: Icons.light_mode_outlined,
                        title: l10n.settingsFollowSystemTheme,
                        palette: palette,
                        trailing: _VocaSwitch(
                          key: const Key('system-theme-switch'),
                          value: settings.themeMode == ThemeMode.system,
                          onChanged: (enabled) => notifier.setThemeMode(
                            enabled ? ThemeMode.system : ThemeMode.light,
                          ),
                        ),
                      ),
                      _SectionGap(palette: palette),
                      _SectionLabel(
                        label: l10n.settingsSectionLanguage,
                        palette: palette,
                      ),
                      _SettingsRow(
                        icon: Icons.language,
                        title: l10n.settingsAppLanguage,
                        palette: palette,
                        trailing: _LanguageSelector(
                          key: const Key('language-dropdown'),
                          languageCode: settings.locale.languageCode,
                          onChanged: notifier.setLocale,
                        ),
                      ),
                      _SectionGap(palette: palette),
                      _SectionLabel(
                        label: l10n.settingsSectionNotifications,
                        palette: palette,
                      ),
                      _SettingsRow(
                        icon: Icons.notifications_none,
                        title: l10n.settingsDailyReminder,
                        subtitle: l10n.settingsDailyReminderSubtitle,
                        palette: palette,
                        showDivider: true,
                        trailing: _VocaSwitch(
                          key: const Key('daily-reminder-switch'),
                          value: settings.dailyReminder,
                          onChanged: notifier.setDailyReminder,
                        ),
                      ),
                      _SettingsRow(
                        icon: Icons.notifications_none,
                        title: l10n.settingsStreakAlert,
                        subtitle: l10n.settingsStreakAlertSubtitle,
                        palette: palette,
                        showDivider: true,
                        trailing: _VocaSwitch(
                          key: const Key('streak-alert-switch'),
                          value: settings.streakAlert,
                          onChanged: notifier.setStreakAlert,
                        ),
                      ),
                      _SettingsRow(
                        icon: Icons.notifications_none,
                        title: l10n.settingsReviewDue,
                        subtitle: l10n.settingsReviewDueSubtitle,
                        palette: palette,
                        trailing: _VocaSwitch(
                          key: const Key('review-due-switch'),
                          value: settings.reviewDueAlert,
                          onChanged: notifier.setReviewDueAlert,
                        ),
                      ),
                      _SectionGap(palette: palette),
                      _SectionLabel(
                        label: l10n.settingsSectionAudio,
                        palette: palette,
                      ),
                      _SettingsRow(
                        icon: Icons.volume_up_outlined,
                        title: l10n.settingsAutoPlayPronunciation,
                        subtitle: l10n.settingsAutoPlayPronunciationSubtitle,
                        palette: palette,
                        showDivider: true,
                        trailing: _VocaSwitch(
                          key: const Key('auto-play-switch'),
                          value: settings.autoPlayPronunciation,
                          onChanged: notifier.setAutoPlayPronunciation,
                        ),
                      ),
                      _SettingsRow(
                        icon: Icons.volume_up_outlined,
                        title: l10n.settingsSoundEffects,
                        subtitle: l10n.settingsSoundEffectsSubtitle,
                        palette: palette,
                        trailing: _VocaSwitch(
                          key: const Key('sound-effects-switch'),
                          value: settings.soundEffects,
                          onChanged: notifier.setSoundEffects,
                        ),
                      ),
                      _SectionGap(palette: palette),
                      _SectionLabel(
                        label: l10n.settingsSectionAccount,
                        palette: palette,
                        muted: true,
                      ),
                      _SettingsRow(
                        icon: Icons.shield_outlined,
                        title: l10n.settingsPrivacyPolicy,
                        palette: palette,
                        showDivider: true,
                        showChevron: true,
                        onTap: () => _showPrivacyPolicy(context),
                      ),
                      _SettingsRow(
                        key: const Key('delete-account-button'),
                        icon: Icons.shield_outlined,
                        title: l10n.settingsDeleteAccount,
                        palette: palette,
                        destructive: true,
                        showChevron: true,
                        trailing: deletingAccount
                            ? const SizedBox.square(
                                dimension: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : null,
                        onTap: deletingAccount
                            ? null
                            : () => _deleteAccount(context, ref),
                      ),
                      _SectionGap(palette: palette),
                      Container(
                        height: 66,
                        color: palette.background,
                        padding: const EdgeInsets.fromLTRB(20, 20, 20, 28),
                        alignment: Alignment.topLeft,
                        child: Text(
                          l10n.settingsVersionLabel(appVersion),
                          style: AppTextStyles.caption.copyWith(
                            color: palette.muted,
                            fontSize: 12,
                            height: 18 / 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Future<void> _showPrivacyPolicy(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (context) => _InformationSheet(
        icon: Icons.shield_outlined,
        title: l10n.settingsPrivacyPolicy,
        body: l10n.settingsPrivacyPolicyBody,
      ),
    );
  }

  Future<void> _deleteAccount(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.settingsDeleteAccountDialogTitle),
        content: Text(l10n.settingsDeleteAccountDialogBody),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: Text(l10n.settingsCancel),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFEF4444),
            ),
            onPressed: () => Navigator.pop(dialogContext, true),
            child: Text(l10n.settingsContinue),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    final deleted = await ref.read(authProvider.notifier).deleteAccount();
    if (!deleted && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            ref.read(authProvider).errorMessage ??
                l10n.settingsDeleteAccountFailed,
          ),
        ),
      );
    }
  }
}

class _SettingsHeader extends StatelessWidget {
  const _SettingsHeader({required this.palette});

  final _SettingsPalette palette;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 17.258),
      decoration: BoxDecoration(
        color: palette.background,
        border: Border(bottom: BorderSide(color: palette.divider, width: 1.25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            key: const Key('settings-back'),
            borderRadius: BorderRadius.circular(8),
            onTap: () {
              if (context.canPop()) context.pop();
            },
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.arrow_back, size: 16, color: palette.muted),
                const SizedBox(width: 4),
                Text(
                  AppLocalizations.of(context)!.settingsBackToProfile,
                  style: AppTextStyles.caption.copyWith(
                    color: palette.muted,
                    fontSize: 14,
                    height: 21 / 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Text(
            AppLocalizations.of(context)!.settingsTitle,
            style: AppTextStyles.heading.copyWith(
              color: palette.text,
              fontSize: 22,
              height: 33 / 22,
              letterSpacing: -.3,
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel({
    required this.label,
    required this.palette,
    this.muted = false,
  });

  final String label;
  final _SettingsPalette palette;
  final bool muted;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: palette.background,
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
      child: Text(
        label,
        style: AppTextStyles.caption.copyWith(
          color: muted ? palette.muted : palette.secondaryText,
          fontSize: 11,
          height: 16 / 11,
          letterSpacing: .45,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _SectionGap extends StatelessWidget {
  const _SectionGap({required this.palette});

  final _SettingsPalette palette;

  @override
  Widget build(BuildContext context) =>
      SizedBox(height: 1, child: ColoredBox(color: palette.divider));
}

class _SettingsRow extends StatelessWidget {
  const _SettingsRow({
    super.key,
    required this.icon,
    required this.title,
    required this.palette,
    this.subtitle,
    this.trailing,
    this.onTap,
    this.showChevron = false,
    this.showDivider = false,
    this.destructive = false,
  });

  final IconData icon;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  final bool showChevron;
  final bool showDivider;
  final bool destructive;
  final _SettingsPalette palette;

  @override
  Widget build(BuildContext context) {
    final foreground = destructive ? const Color(0xFFDC2626) : palette.text;
    return InkWell(
      onTap: onTap,
      child: Container(
        constraints: const BoxConstraints(minHeight: 64),
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        decoration: BoxDecoration(
          color: palette.background,
          border: showDivider
              ? Border(bottom: BorderSide(color: palette.divider, width: 1.258))
              : null,
        ),
        child: Row(
          children: [
            Container(
              width: 32,
              height: 32,
              decoration: BoxDecoration(
                color: destructive
                    ? const Color(0xFFFEF2F2)
                    : palette.iconBackground,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(
                icon,
                size: 16,
                color: destructive
                    ? const Color(0xFFDC2626)
                    : AppColors.primary,
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
                      color: foreground,
                      fontSize: 14,
                      height: 21 / 14,
                      fontWeight: FontWeight.w400,
                    ),
                  ),
                  if (subtitle != null)
                    Text(
                      subtitle!,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.caption.copyWith(
                        color: palette.secondaryText,
                        fontSize: 12,
                        height: 18 / 12,
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(width: 10),
            trailing ??
                (showChevron
                    ? Icon(Icons.chevron_right, size: 17, color: palette.muted)
                    : const SizedBox.shrink()),
          ],
        ),
      ),
    );
  }
}

class _LanguageSelector extends StatelessWidget {
  const _LanguageSelector({
    super.key,
    required this.languageCode,
    required this.onChanged,
  });

  final String languageCode;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 81,
      height: 34.5,
      padding: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        border: Border.all(color: const Color(0xFFECEAF4), width: 1.258),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _LanguageChoice(
            label: 'EN',
            selected: languageCode == 'en',
            onTap: () => onChanged('en'),
          ),
          _LanguageChoice(
            label: 'VI',
            selected: languageCode != 'en',
            onTap: () => onChanged('vi'),
          ),
        ],
      ),
    );
  }
}

class _LanguageChoice extends StatelessWidget {
  const _LanguageChoice({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(10),
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        width: selected ? 41.98 : 36.48,
        height: 32,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : Colors.transparent,
          borderRadius: BorderRadius.zero,
        ),
        child: Text(
          label,
          style: AppTextStyles.label.copyWith(
            color: selected ? Colors.white : const Color(0xFF9C99B5),
            fontSize: 14,
            height: 20 / 14,
            fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
          ),
        ),
      ),
    );
  }
}

class _VocaSwitch extends StatelessWidget {
  const _VocaSwitch({super.key, required this.value, required this.onChanged});

  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      toggled: value,
      button: true,
      child: InkWell(
        borderRadius: BorderRadius.circular(15),
        onTap: () => onChanged(!value),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          curve: Curves.easeOut,
          width: 44,
          height: 24,
          padding: const EdgeInsets.all(2),
          decoration: BoxDecoration(
            color: value ? AppColors.primary : const Color(0xFFECEAF4),
            borderRadius: BorderRadius.circular(15),
          ),
          child: AnimatedAlign(
            duration: const Duration(milliseconds: 180),
            curve: Curves.easeOut,
            alignment: value ? Alignment.centerRight : Alignment.centerLeft,
            child: Container(
              width: 20,
              height: 20,
              decoration: const BoxDecoration(
                color: Colors.white,
                shape: BoxShape.circle,
                boxShadow: [
                  BoxShadow(
                    color: Color(0x26000000),
                    blurRadius: 3,
                    offset: Offset(0, 1),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _InformationSheet extends StatelessWidget {
  const _InformationSheet({
    required this.icon,
    required this.title,
    required this.body,
  });

  final IconData icon;
  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 8, 24, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: AppColors.primary, size: 28),
            const SizedBox(height: 12),
            Text(title, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            Text(body, style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 22),
            FilledButton(
              onPressed: () => Navigator.pop(context),
              child: Text(AppLocalizations.of(context)!.settingsDone),
            ),
          ],
        ),
      ),
    );
  }
}

class _SettingsPalette {
  const _SettingsPalette({
    required this.background,
    required this.text,
    required this.secondaryText,
    required this.muted,
    required this.divider,
    required this.iconBackground,
  });

  final Color background;
  final Color text;
  final Color secondaryText;
  final Color muted;
  final Color divider;
  final Color iconBackground;

  factory _SettingsPalette.of(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    return _SettingsPalette(
      background: dark ? const Color(0xFF17151F) : Colors.white,
      text: dark ? const Color(0xFFF7F5FF) : const Color(0xFF14142B),
      secondaryText: dark ? const Color(0xFFB8B3C8) : const Color(0xFF444444),
      muted: dark ? const Color(0xFF8F899F) : const Color(0xFF9C99B5),
      divider: dark ? const Color(0xFF302C3B) : const Color(0xFFECEAF4),
      iconBackground: dark ? const Color(0xFF2D2647) : const Color(0xFFF6F5FB),
    );
  }
}
