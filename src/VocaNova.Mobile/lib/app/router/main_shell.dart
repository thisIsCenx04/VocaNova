import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/widgets/offline_banner.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

/// Chỉ số nhánh (tab) Practice trong bottom navbar.
const _practiceBranchIndex = 3;

class MainShell extends ConsumerWidget {
  const MainShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isOnline = ref.watch(connectivityProvider).value ?? true;
    return AnnotatedRegion<SystemUiOverlayStyle>(
      // Without this the OS draws its own (light) system navigation bar
      // background below our purple bottom nav instead of matching it.
      value: const SystemUiOverlayStyle(
        systemNavigationBarColor: AppColors.primary,
        systemNavigationBarIconBrightness: Brightness.light,
      ),
      child: Scaffold(
        body: Column(
          children: [
            if (!isOnline)
              const SafeArea(bottom: false, child: OfflineBanner()),
            Expanded(child: navigationShell),
          ],
        ),
        bottomNavigationBar: _VocaNovaBottomNav(
          currentIndex: navigationShell.currentIndex,
          onSelected: (index) {
            navigationShell.goBranch(
              index,
              initialLocation: index == navigationShell.currentIndex,
            );
            // Vào lại tab Practice thì tải lại nguồn kiểm tra để số từ luôn
            // khớp với danh sách hiện tại (ví dụ vừa thêm/xóa từ ở nơi khác).
            if (index == _practiceBranchIndex) {
              ref.read(quizConfigProvider.notifier).loadSources();
            }
          },
        ),
      ),
    );
  }
}

class _VocaNovaBottomNav extends StatelessWidget {
  const _VocaNovaBottomNav({
    required this.currentIndex,
    required this.onSelected,
  });

  final int currentIndex;
  final ValueChanged<int> onSelected;

  static const _icons = [
    (Icons.home_outlined, Icons.home),
    (Icons.search, Icons.search),
    (Icons.menu_book_outlined, Icons.menu_book),
    (Icons.bolt_outlined, Icons.bolt),
    (Icons.person_outline, Icons.person),
  ];

  static const _contentHeight = 63.0; // 9 top padding + 54 item height

  @override
  Widget build(BuildContext context) {
    // Fall back to 24 so devices with a 3-button nav bar (near-zero inset)
    // still get breathing room below the labels, but grow for gesture-bar
    // devices instead of leaving them under-padded.
    final bottomPadding = math.max(24.0, MediaQuery.paddingOf(context).bottom);
    final l10n = AppLocalizations.of(context)!;
    final labels = [
      l10n.navHome,
      l10n.navSearch,
      l10n.navLists,
      l10n.navPractice,
      l10n.navProfile,
    ];
    return Container(
      height: _contentHeight + bottomPadding,
      color: AppColors.primary,
      padding: EdgeInsets.fromLTRB(8, 9, 8, bottomPadding),
      child: Row(
        children: [
          for (var index = 0; index < _icons.length; index++)
            Expanded(
              child: _BottomNavItem(
                icon: _icons[index].$1,
                selectedIcon: _icons[index].$2,
                label: labels[index],
                selected: index == currentIndex,
                onTap: () => onSelected(index),
              ),
            ),
        ],
      ),
    );
  }
}

class _BottomNavItem extends StatelessWidget {
  const _BottomNavItem({
    required this.icon,
    required this.selectedIcon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final IconData icon;
  final IconData selectedIcon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Container(
          height: 54,
          decoration: BoxDecoration(
            color: selected ? Colors.black.withValues(alpha: 0.5) : null,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                selected ? selectedIcon : icon,
                color: Colors.white,
                size: selected ? 22 : 21,
              ),
              const SizedBox(height: 4),
              Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: AppTextStyles.caption.copyWith(
                  color: Colors.white,
                  fontSize: 10,
                  height: 15 / 10,
                  fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
