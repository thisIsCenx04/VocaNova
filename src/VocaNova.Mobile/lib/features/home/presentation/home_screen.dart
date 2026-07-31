import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/home/application/home_daily_word_provider.dart';
import 'package:vocanova_mobile/features/home/application/home_topics_provider.dart';
import 'package:vocanova_mobile/features/home/domain/personal_topic_recommendation.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class _HomePalette {
  const _HomePalette({
    required this.background,
    required this.surface,
    required this.elevatedSurface,
    required this.text,
    required this.secondaryText,
    required this.border,
    required this.dailyGoal,
    required this.wordOfTheDay,
  });

  final Color background;
  final Color surface;
  final Color elevatedSurface;
  final Color text;
  final Color secondaryText;
  final Color border;
  final Color dailyGoal;
  final Color wordOfTheDay;

  bool get isDark => background == AppColors.background;

  factory _HomePalette.of(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    return _HomePalette(
      background: dark ? AppColors.background : AppColors.lightBackground,
      surface: dark ? AppColors.surface : Colors.white,
      elevatedSurface: dark ? const Color(0xFF2A2A2A) : const Color(0xFFF7F7F5),
      text: dark ? AppColors.onSurface : Colors.black,
      secondaryText: dark ? const Color(0xFFB8B3C3) : const Color(0xFF888888),
      border: dark ? const Color(0xFF37343D) : const Color(0xFFE6E6E6),
      dailyGoal: dark ? const Color(0xFF29371F) : const Color(0xFFDCEEB1),
      wordOfTheDay: dark ? const Color(0xFF35284D) : const Color(0xFFC5B0F4),
    );
  }
}

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(_loadData);
  }

  void _loadData() {
    if (!mounted) return;
    if (ref.read(authProvider).user == null) {
      ref.read(authProvider.notifier).loadCurrentUser();
    }
    ref.read(progressOverviewProvider.notifier).load();
    ref.read(listsProvider.notifier).load();
    ref.read(wrongWordsProvider.notifier).load();
  }

  Future<void> _refresh() async {
    ref.invalidate(homeTopicsProvider);
    ref.invalidate(homeDailyWordProvider);
    await Future.wait([
      ref.read(progressOverviewProvider.notifier).load(),
      ref.read(listsProvider.notifier).load(),
      ref.read(wrongWordsProvider.notifier).load(),
      ref.read(authProvider.notifier).loadCurrentUser(),
    ]);
  }

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    final l10n = AppLocalizations.of(context)!;
    final displayName = ref.watch(
      authProvider.select((state) => state.user?.displayName),
    );
    return Scaffold(
      backgroundColor: palette.background,
      body: SafeArea(
        bottom: false,
        child: RefreshIndicator(
          onRefresh: _refresh,
          color: AppColors.primary,
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              _HomeHeader(
                onNotification: () => context.push(AppRoutes.notifications),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(25, 17, 25, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      _greeting(l10n),
                      style: AppTextStyles.caption.copyWith(
                        color: palette.secondaryText,
                        fontSize: 13,
                        height: 19.5 / 13,
                      ),
                    ),
                    Text(
                      displayName == null
                          ? l10n.homeWelcomeBack
                          : l10n.homeGreetingName(displayName),
                      style: AppTextStyles.heading.copyWith(
                        color: palette.text,
                        fontSize: 28,
                        height: 32.2 / 28,
                        letterSpacing: -0.7,
                      ),
                    ),
                    const SizedBox(height: 13),
                    _SearchPill(onTap: () => context.go(AppRoutes.search)),
                    const SizedBox(height: 16),
                    const _DailyGoalCard(),
                    const SizedBox(height: 15),
                    const _WordOfTheDayCard(),
                    const SizedBox(height: 14),
                    const _StatsRow(),
                    const SizedBox(height: 16),
                    const _ContinueCard(),
                    const _QuickActionsCard(),
                    const SizedBox(height: 16),
                    const _TopicsForYou(),
                    const _OverdueBanner(),
                    const _MyListsCard(),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static String _greeting(AppLocalizations l10n) {
    final hour = DateTime.now().hour;
    if (hour < 12) return l10n.homeGreetingMorning;
    if (hour < 18) return l10n.homeGreetingAfternoon;
    return l10n.homeGreetingEvening;
  }
}

/// Section title with an optional "See all" trailing action.
class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title, this.onSeeAll});

  final String title;
  final VoidCallback? onSeeAll;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Expanded(
            child: Text(
              title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: AppTextStyles.heading.copyWith(
                color: palette.text,
                fontSize: 17,
                height: 22 / 17,
                letterSpacing: -0.3,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          if (onSeeAll != null) const SizedBox(width: 8),
          if (onSeeAll != null)
            Material(
              color: palette.elevatedSurface,
              borderRadius: BorderRadius.circular(999),
              child: InkWell(
                borderRadius: BorderRadius.circular(999),
                onTap: onSeeAll,
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 6,
                  ),
                  child: Text(
                    AppLocalizations.of(context)!.homeSeeAll,
                    style: AppTextStyles.caption.copyWith(
                      color: palette.text,
                      fontSize: 12,
                      height: 16 / 12,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

/// Reusable outlined section container matching the Home card language.
class _HomeCard extends StatelessWidget {
  const _HomeCard({required this.child, this.onTap});

  final Widget child;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    final content = Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border.all(color: palette.border, width: 1.25),
        borderRadius: BorderRadius.circular(16),
      ),
      child: child,
    );
    if (onTap == null) return content;
    return Material(
      color: Colors.transparent,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: content,
      ),
    );
  }
}

class _HomeHeader extends StatelessWidget {
  const _HomeHeader({required this.onNotification});

  final VoidCallback onNotification;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return Container(
      height: 56,
      padding: const EdgeInsets.fromLTRB(32, 0, 25, 0),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border(bottom: BorderSide(color: palette.border, width: 1)),
      ),
      child: Row(
        children: [
          _VocaNovaLogo(color: palette.text),
          const SizedBox(width: 9),
          Text(
            'VocaNova',
            style: AppTextStyles.button.copyWith(
              color: palette.text,
              fontSize: 16,
              height: 24 / 16,
              fontWeight: FontWeight.w700,
            ),
          ),
          const Spacer(),
          _NotificationButton(onPressed: onNotification),
        ],
      ),
    );
  }
}

class _VocaNovaLogo extends StatelessWidget {
  const _VocaNovaLogo({required this.color});

  final Color color;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 31,
      height: 31,
      child: CustomPaint(painter: _LogoPainter(color)),
    );
  }
}

class _LogoPainter extends CustomPainter {
  const _LogoPainter(this.color);

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3
      ..strokeCap = StrokeCap.square;
    final path = Path()
      ..moveTo(size.width * 0.86, size.height * 0.25)
      ..lineTo(size.width * 0.38, size.height * 0.25)
      ..lineTo(size.width * 0.38, size.height * 0.73);
    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant _LogoPainter oldDelegate) {
    return oldDelegate.color != color;
  }
}

class _NotificationButton extends ConsumerWidget {
  const _NotificationButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final palette = _HomePalette.of(context);
    // .asData avoids rethrowing while loading or on error (e.g. offline) —
    // the badge simply hides until a real count arrives.
    final unread =
        ref.watch(notificationsUnreadCountProvider).asData?.value ?? 0;
    return Stack(
      clipBehavior: Clip.none,
      children: [
        Material(
          color: palette.elevatedSurface,
          shape: const CircleBorder(),
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: onPressed,
            child: SizedBox(
              width: 40,
              height: 40,
              child: Icon(
                Icons.notifications_none,
                size: 17,
                color: palette.text,
              ),
            ),
          ),
        ),
        if (unread > 0)
          Positioned(
            top: 9,
            right: 8,
            child: Container(
              width: 8,
              height: 8,
              decoration: const BoxDecoration(
                color: Color(0xFFFF3D8B),
                shape: BoxShape.circle,
              ),
            ),
          ),
      ],
    );
  }
}

class _SearchPill extends StatelessWidget {
  const _SearchPill({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return Material(
      color: palette.elevatedSurface,
      borderRadius: BorderRadius.circular(50),
      child: InkWell(
        borderRadius: BorderRadius.circular(50),
        onTap: onTap,
        child: Container(
          height: 44,
          padding: const EdgeInsets.fromLTRB(15, 0, 12, 0),
          decoration: BoxDecoration(
            border: Border.all(color: palette.border, width: 1.25),
            borderRadius: BorderRadius.circular(50),
          ),
          child: Row(
            children: [
              Icon(Icons.search, size: 16, color: palette.secondaryText),
              const SizedBox(width: 12),
              Text(
                AppLocalizations.of(context)!.homeSearchHint,
                style: AppTextStyles.caption.copyWith(
                  color: palette.secondaryText,
                  fontSize: 14,
                  height: 21 / 14,
                ),
              ),
              const Spacer(),
              Container(
                width: 35,
                height: 20,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: palette.border,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  '⌘K',
                  style: AppTextStyles.caption.copyWith(
                    color: palette.secondaryText,
                    fontSize: 10,
                    height: 15 / 10,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DailyGoalCard extends ConsumerWidget {
  const _DailyGoalCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final palette = _HomePalette.of(context);
    final l10n = AppLocalizations.of(context)!;
    final foreground = palette.isDark ? AppColors.onSurface : Colors.black;
    final summary = ref.watch(
      progressOverviewProvider.select((state) => state.summary),
    );
    final total = summary?.totalAnswers7Days ?? 0;
    final correct = summary?.correct7Days ?? 0;
    final streak = summary?.currentStreakDays ?? 0;
    final progress = total == 0 ? 0.0 : (correct / total).clamp(0.0, 1.0);
    final goalText = summary == null ? '— / —' : '$correct / $total';
    final streakText = streak > 0
        ? l10n.homeStreakActive(streak)
        : l10n.homeStreakInactive;
    return Container(
      constraints: const BoxConstraints(minHeight: 142),
      padding: const EdgeInsets.fromLTRB(19, 17, 19, 13),
      decoration: BoxDecoration(
        color: palette.dailyGoal,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.track_changes, size: 12, color: foreground),
              const SizedBox(width: 6),
              Text(
                l10n.progressAccuracy7DaysLabel.toUpperCase(),
                style: AppTextStyles.caption.copyWith(
                  color: foreground,
                  fontSize: 10,
                  height: 15 / 10,
                  letterSpacing: 0.6,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
          const SizedBox(height: 13),
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              _DashedGoalRing(foreground: foreground, progress: progress),
              const SizedBox(width: 19),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      goalText,
                      style: AppTextStyles.button.copyWith(
                        color: foreground,
                        fontSize: 18,
                        height: 27 / 18,
                        letterSpacing: -0.3,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    Text(
                      l10n.homeStatAccuracy,
                      style: AppTextStyles.caption.copyWith(
                        color: palette.isDark
                            ? palette.secondaryText
                            : const Color(0xFF444444),
                        fontSize: 13,
                        height: 19.5 / 13,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Icon(
                          Icons.local_fire_department,
                          size: 12,
                          color: foreground,
                        ),
                        const SizedBox(width: 5),
                        Expanded(
                          child: Text(
                            streakText,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: AppTextStyles.caption.copyWith(
                              color: foreground,
                              fontSize: 11,
                              height: 16.5 / 11,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _DashedGoalRing extends StatelessWidget {
  const _DashedGoalRing({required this.foreground, required this.progress});

  final Color foreground;
  final double progress;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 62,
      height: 62,
      child: Stack(
        alignment: Alignment.center,
        children: [
          TweenAnimationBuilder<double>(
            tween: Tween(begin: 0, end: progress),
            duration: const Duration(milliseconds: 700),
            curve: Curves.easeOutCubic,
            builder: (context, value, _) => CustomPaint(
              size: const Size.square(62),
              painter: _DashedRingPainter(
                progress: value,
                foreground: foreground,
              ),
            ),
          ),
          Text(
            '${(progress * 100).round()}%',
            style: AppTextStyles.caption.copyWith(
              color: foreground,
              fontSize: 13,
              height: 19.5 / 13,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _DashedRingPainter extends CustomPainter {
  const _DashedRingPainter({required this.progress, required this.foreground});

  final double progress;
  final Color foreground;

  @override
  void paint(Canvas canvas, Size size) {
    final rect = Offset.zero & size;
    final basePaint = Paint()
      ..color = foreground.withValues(alpha: 0.12)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 4
      ..strokeCap = StrokeCap.round;
    final progressPaint = Paint()
      ..color = foreground
      ..style = PaintingStyle.stroke
      ..strokeWidth = 4
      ..strokeCap = StrokeCap.round;
    const gap = math.pi / 5;
    final segment = (math.pi * 2 - gap * 4) / 4;
    var start = -math.pi / 2;
    for (var i = 0; i < 4; i++) {
      canvas.drawArc(rect.deflate(4), start, segment, false, basePaint);
      start += segment + gap;
    }
    canvas.drawArc(
      rect.deflate(4),
      -math.pi / 2,
      math.pi * 2 * progress,
      false,
      progressPaint,
    );
  }

  @override
  bool shouldRepaint(covariant _DashedRingPainter oldDelegate) {
    return oldDelegate.progress != progress ||
        oldDelegate.foreground != foreground;
  }
}

class _WordOfTheDayCard extends ConsumerWidget {
  const _WordOfTheDayCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final palette = _HomePalette.of(context);
    final l10n = AppLocalizations.of(context)!;
    final foreground = palette.isDark ? AppColors.onSurface : Colors.black;
    final dailyWord = ref.watch(homeDailyWordProvider);
    final word = dailyWord.asData?.value;
    final sense = word?.senses.firstOrNull;
    final phonetic = word?.phoneticUk ?? word?.phoneticUs;

    Future<void> playPronunciation() async {
      if (word == null) return;
      try {
        final ukAudio = word.audio
            .where((item) => item.accent.toUpperCase() == 'UK')
            .firstOrNull;
        final audio = ukAudio ?? word.audio.firstOrNull;
        await ref
            .read(audioPlaybackServiceProvider)
            .playPronunciation(
              word: word.word,
              accent: 'UK',
              audioUrl: audio?.url,
            );
      } catch (_) {
        if (!context.mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.homePronunciationPlayError)),
        );
      }
    }

    return Container(
      constraints: const BoxConstraints(minHeight: 225),
      padding: const EdgeInsets.fromLTRB(19, 18, 19, 17),
      decoration: BoxDecoration(
        color: palette.wordOfTheDay,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                l10n.homeWordOfTheDayLabel,
                style: AppTextStyles.caption.copyWith(
                  color: foreground,
                  fontSize: 10,
                  height: 15 / 10,
                  letterSpacing: 0.6,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const Spacer(),
              Material(
                color: foreground.withValues(alpha: 0.15),
                shape: const CircleBorder(),
                child: IconButton(
                  key: const Key('daily-word-audio'),
                  tooltip: l10n.homePronunciationPlayTooltip,
                  onPressed: word == null ? null : playPronunciation,
                  constraints: const BoxConstraints.tightFor(
                    width: 32,
                    height: 32,
                  ),
                  padding: EdgeInsets.zero,
                  icon: Icon(
                    Icons.volume_up_outlined,
                    size: 16,
                    color: foreground,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(
            word?.word ??
                (dailyWord.isLoading
                    ? l10n.homeDailyWordLoading
                    : l10n.homeDailyWordUnavailable),
            style: AppTextStyles.heading.copyWith(
              color: foreground,
              fontSize: 28,
              height: 33.6 / 28,
              letterSpacing: -0.9,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            [
              if (phonetic?.trim().isNotEmpty == true) phonetic!,
              if (sense?.wordClass.trim().isNotEmpty == true) sense!.wordClass,
            ].join(' · '),
            style: AppTextStyles.caption.copyWith(
              color: foreground,
              fontSize: 13,
              height: 19.5 / 13,
              fontStyle: FontStyle.italic,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            sense?.englishDefinition ??
                (dailyWord.hasError
                    ? l10n.homeDailyWordLoadError
                    : l10n.homeDailyWordChoosing),
            maxLines: 3,
            overflow: TextOverflow.ellipsis,
            style: AppTextStyles.caption.copyWith(
              color: palette.isDark
                  ? palette.secondaryText
                  : const Color(0xFF252238),
              fontSize: 13,
              height: 19.5 / 13,
            ),
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: 135,
            height: 35,
            child: ElevatedButton(
              onPressed: word == null
                  ? null
                  : () => context.push(
                      AppRoutes.wordDetail(word.wordId.toString()),
                    ),
              style: ElevatedButton.styleFrom(
                backgroundColor: palette.isDark
                    ? AppColors.primary
                    : Colors.black,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(horizontal: 12),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(999),
                ),
                textStyle: AppTextStyles.caption.copyWith(
                  fontSize: 12,
                  height: 18 / 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              child: FittedBox(
                fit: BoxFit.scaleDown,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(l10n.homeLearnThisWord),
                    const SizedBox(width: 6),
                    const Icon(Icons.chevron_right, size: 14),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatsRow extends ConsumerWidget {
  const _StatsRow();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final summary = ref.watch(
      progressOverviewProvider.select((state) => state.summary),
    );
    final loading = ref.watch(
      progressOverviewProvider.select((state) => state.isLoading),
    );
    String words = '—';
    String accuracy = '—';
    String mastered = '—';
    if (summary != null) {
      words = '${summary.totalWordsInProgress}';
      accuracy = '${summary.accuracy7Days.round()}%';
      mastered = '${summary.masteredWords}';
    }
    return Row(
      children: [
        Expanded(
          child: _StatCard(
            icon: Icons.menu_book_outlined,
            iconColor: AppColors.primary,
            value: loading && summary == null ? '—' : words,
            label: l10n.homeStatWords,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _StatCard(
            icon: Icons.trending_up,
            iconColor: const Color(0xFF20C7A3),
            value: loading && summary == null ? '—' : accuracy,
            label: l10n.homeStatAccuracy,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _StatCard(
            icon: Icons.emoji_events_outlined,
            iconColor: const Color(0xFFFFA425),
            value: loading && summary == null ? '—' : mastered,
            label: l10n.homeStatMastered,
          ),
        ),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.icon,
    required this.iconColor,
    required this.value,
    required this.label,
  });

  final IconData icon;
  final Color iconColor;
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return Container(
      constraints: const BoxConstraints(minHeight: 105),
      padding: const EdgeInsets.fromLTRB(14, 18, 14, 14),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border.all(color: palette.border, width: 1.25),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 26,
            height: 26,
            decoration: BoxDecoration(
              color: palette.elevatedSurface,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 14, color: iconColor),
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: AppTextStyles.heading.copyWith(
              color: palette.text,
              fontSize: 20,
              height: 24 / 20,
              letterSpacing: -0.3,
            ),
          ),
          Text(
            label,
            style: AppTextStyles.caption.copyWith(
              color: palette.secondaryText,
              fontSize: 11,
              height: 16.5 / 11,
            ),
          ),
        ],
      ),
    );
  }
}

/// Dark "Continue" card that resumes the user's most recent list.
class _ContinueCard extends ConsumerWidget {
  const _ContinueCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final state = ref.watch(listsProvider);
    if (state.isLoading && state.lists.isEmpty) {
      return const _ContinueSkeleton();
    }
    if (state.lists.isEmpty) return const SizedBox.shrink();
    final list = state.lists.first;
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Material(
        color: const Color(0xFF1E1B2E),
        borderRadius: BorderRadius.circular(18),
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: () =>
              context.push(AppRoutes.listDetail(list.listId.toString())),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(18, 18, 16, 18),
            child: Row(
              children: [
                Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.08),
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: Colors.white.withValues(alpha: 0.25),
                      width: 1.5,
                    ),
                  ),
                  child: const Icon(
                    Icons.menu_book_outlined,
                    size: 20,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        l10n.homeContinueLabel,
                        style: AppTextStyles.caption.copyWith(
                          color: Colors.white.withValues(alpha: 0.55),
                          fontSize: 10,
                          height: 15 / 10,
                          letterSpacing: 0.8,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        list.listName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: AppTextStyles.button.copyWith(
                          color: Colors.white,
                          fontSize: 17,
                          height: 22 / 17,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        l10n.homeWordCount(list.wordCount),
                        style: AppTextStyles.caption.copyWith(
                          color: Colors.white.withValues(alpha: 0.6),
                          fontSize: 12,
                          height: 17 / 12,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_right,
                  color: Colors.white.withValues(alpha: 0.6),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ContinueSkeleton extends StatelessWidget {
  const _ContinueSkeleton();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 82,
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: const Color(0xFF1E1B2E),
        borderRadius: BorderRadius.circular(18),
      ),
      child: const Center(
        child: SizedBox(
          width: 22,
          height: 22,
          child: CircularProgressIndicator(
            strokeWidth: 2.4,
            color: Colors.white54,
          ),
        ),
      ),
    );
  }
}

/// Quick action shortcuts: Quiz, Review, Topics.
class _QuickActionsCard extends StatelessWidget {
  const _QuickActionsCard();

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    final l10n = AppLocalizations.of(context)!;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 18),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border.all(color: palette.border, width: 1.25),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.homeQuickActionsTitle,
            style: AppTextStyles.heading.copyWith(
              color: palette.text,
              fontSize: 16,
              height: 21 / 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: _QuickAction(
                  icon: Icons.bolt,
                  iconColor: AppColors.primary,
                  label: l10n.homeActionQuiz,
                  onTap: () => context.go(AppRoutes.quizConfig),
                ),
              ),
              Expanded(
                child: _QuickAction(
                  icon: Icons.replay,
                  iconColor: const Color(0xFFFF6B6B),
                  label: l10n.homeActionReview,
                  onTap: () => context.push(AppRoutes.wrongWords),
                ),
              ),
              Expanded(
                child: _QuickAction(
                  icon: Icons.menu_book_outlined,
                  iconColor: const Color(0xFF20C7A3),
                  label: l10n.homeActionTopics,
                  onTap: () => context.push(AppRoutes.topics),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  const _QuickAction({
    required this.icon,
    required this.iconColor,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final Color iconColor;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return InkWell(
      borderRadius: BorderRadius.circular(12),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Column(
          children: [
            Container(
              width: 46,
              height: 46,
              decoration: BoxDecoration(
                color: iconColor.withValues(alpha: 0.12),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 21, color: iconColor),
            ),
            const SizedBox(height: 8),
            Text(
              label,
              style: AppTextStyles.caption.copyWith(
                color: palette.text,
                fontSize: 13,
                height: 18 / 13,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Horizontal "Topics for you" carousel loaded from the topic catalogue.
class _TopicsForYou extends ConsumerWidget {
  const _TopicsForYou();

  static const _emoji = ['🎓', '💼', '💻', '🗣️', '🔬', '✈️', '🍽️', '⚽'];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final topicsAsync = ref.watch(homeTopicsProvider);
    return topicsAsync.when(
      loading: () => const _TopicsSection(child: _TopicsSkeleton()),
      error: (_, _) => const SizedBox.shrink(),
      data: (topics) {
        if (topics.isEmpty) {
          final l10n = AppLocalizations.of(context)!;
          return _TopicsSection(
            onSeeAll: () => context.push(AppRoutes.topics),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(l10n.homeTopicsForYouEmpty),
                const SizedBox(height: 4),
                TextButton(
                  onPressed: () => context.push(AppRoutes.topics),
                  child: Text(l10n.homeExploreTopics),
                ),
              ],
            ),
          );
        }
        final shown = topics.take(8).toList();
        return _TopicsSection(
          onSeeAll: () => context.push(AppRoutes.topics),
          child: SizedBox(
            height: 150,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: EdgeInsets.zero,
              itemCount: shown.length,
              separatorBuilder: (_, _) => const SizedBox(width: 12),
              itemBuilder: (context, index) {
                final topic = shown[index];
                return _TopicCard(
                  topic: topic,
                  emoji: topic.icon?.trim().isNotEmpty == true
                      ? topic.icon!.trim()
                      : _emoji[index % _emoji.length],
                  onTap: () => context.push(
                    AppRoutes.topicDetail(topic.topicId.toString()),
                    extra: TopicSummary(
                      topicId: topic.topicId,
                      name: topic.name,
                      nameVi: topic.nameVi,
                      icon: topic.icon,
                      wordCount: topic.wordCount,
                    ),
                  ),
                );
              },
            ),
          ),
        );
      },
    );
  }
}

class _TopicsSection extends StatelessWidget {
  const _TopicsSection({required this.child, this.onSeeAll});

  final Widget child;
  final VoidCallback? onSeeAll;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionHeader(
            title: AppLocalizations.of(context)!.homeTopicsForYouTitle,
            onSeeAll: onSeeAll,
          ),
          child,
        ],
      ),
    );
  }
}

class _TopicCard extends StatelessWidget {
  const _TopicCard({
    required this.topic,
    required this.emoji,
    required this.onTap,
  });

  final PersonalTopicRecommendation topic;
  final String emoji;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return SizedBox(
      width: 128,
      child: Material(
        color: palette.surface,
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: onTap,
          child: Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              border: Border.all(color: palette.border, width: 1.25),
              borderRadius: BorderRadius.circular(14),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(emoji, style: const TextStyle(fontSize: 26)),
                const Spacer(),
                Text(
                  Localizations.localeOf(context).languageCode == 'vi' &&
                          topic.nameVi?.trim().isNotEmpty == true
                      ? topic.nameVi!
                      : topic.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: AppTextStyles.button.copyWith(
                    color: palette.text,
                    fontSize: 14,
                    height: 18 / 14,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  AppLocalizations.of(
                    context,
                  )!.homeTopicWordCount(topic.wordCount),
                  style: AppTextStyles.caption.copyWith(
                    color: palette.secondaryText,
                    fontSize: 11,
                    height: 15 / 11,
                  ),
                ),
                if (topic.words.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Wrap(
                    spacing: 6,
                    children: [
                      for (final word in topic.words.take(2))
                        Text(
                          word.word,
                          style: AppTextStyles.caption.copyWith(
                            color: palette.secondaryText,
                            fontSize: 10,
                          ),
                        ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TopicsSkeleton extends StatelessWidget {
  const _TopicsSkeleton();

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return SizedBox(
      height: 118,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: EdgeInsets.zero,
        itemCount: 3,
        separatorBuilder: (_, _) => const SizedBox(width: 12),
        itemBuilder: (_, _) => Container(
          width: 128,
          decoration: BoxDecoration(
            color: palette.elevatedSurface,
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
    );
  }
}

/// Peach banner surfacing words that need review (wrong-words list).
class _OverdueBanner extends ConsumerWidget {
  const _OverdueBanner();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final state = ref.watch(wrongWordsProvider);
    if (state.isLoading && state.words.isEmpty) return const SizedBox.shrink();
    final count = state.words.length;
    if (count == 0) return const SizedBox.shrink();
    final countLabel = state.hasMore ? '$count+' : '$count';
    final palette = _HomePalette.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Material(
        color: palette.isDark
            ? const Color(0xFF3A2A24)
            : const Color(0xFFF4CDBD),
        borderRadius: BorderRadius.circular(16),
        child: InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: () => context.push(AppRoutes.wrongWords),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 14, 16),
            child: Row(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.08),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    Icons.replay,
                    size: 19,
                    color: palette.isDark ? Colors.white : Colors.black87,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        l10n.homeWordsToReviewLabel(countLabel),
                        style: AppTextStyles.button.copyWith(
                          color: palette.isDark ? Colors.white : Colors.black,
                          fontSize: 16,
                          height: 21 / 16,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        l10n.homeTapToReviewMistakes,
                        style: AppTextStyles.caption.copyWith(
                          color: palette.isDark
                              ? Colors.white70
                              : const Color(0xFF6B4A3C),
                          fontSize: 12,
                          height: 17 / 12,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_right,
                  color: palette.isDark ? Colors.white70 : Colors.black54,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// "My lists" card with the user's lists and word counts.
class _MyListsCard extends ConsumerWidget {
  const _MyListsCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final state = ref.watch(listsProvider);
    final palette = _HomePalette.of(context);
    if (state.isLoading && state.lists.isEmpty) {
      return _HomeCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _SectionHeader(title: l10n.homeMyListsTitle),
            const Center(
              child: Padding(
                padding: EdgeInsets.all(8),
                child: SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(strokeWidth: 2.4),
                ),
              ),
            ),
          ],
        ),
      );
    }
    if (state.lists.isEmpty) {
      return _HomeCard(
        onTap: () => context.go(AppRoutes.lists),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _SectionHeader(title: l10n.homeMyListsTitle),
            Row(
              children: [
                Icon(Icons.add_circle_outline, color: AppColors.primary),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    l10n.homeCreateFirstList,
                    style: AppTextStyles.caption.copyWith(
                      color: palette.secondaryText,
                      fontSize: 13,
                      height: 18 / 13,
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      );
    }
    final lists = state.lists.take(3).toList();
    return _HomeCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionHeader(
            title: l10n.homeMyListsTitle,
            onSeeAll: () => context.go(AppRoutes.lists),
          ),
          for (var i = 0; i < lists.length; i++) ...[
            if (i > 0)
              Divider(height: 20, color: palette.border.withValues(alpha: 0.7)),
            _MyListRow(
              list: lists[i],
              color: _listColors[i % _listColors.length],
            ),
          ],
        ],
      ),
    );
  }

  static const _listColors = [
    AppColors.primary,
    Color(0xFFFF6B6B),
    Color(0xFF20C7A3),
  ];
}

class _MyListRow extends StatelessWidget {
  const _MyListRow({required this.list, required this.color});

  final UserList list;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final palette = _HomePalette.of(context);
    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(10),
        onTap: () => context.push(AppRoutes.listDetail(list.listId.toString())),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(
            children: [
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.12),
                  shape: BoxShape.circle,
                ),
                child: Icon(Icons.menu_book_outlined, size: 18, color: color),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      list.listName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: AppTextStyles.button.copyWith(
                        color: palette.text,
                        fontSize: 14,
                        height: 19 / 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    Text(
                      AppLocalizations.of(
                        context,
                      )!.homeWordCount(list.wordCount),
                      style: AppTextStyles.caption.copyWith(
                        color: palette.secondaryText,
                        fontSize: 12,
                        height: 16 / 12,
                      ),
                    ),
                  ],
                ),
              ),
              Icon(Icons.chevron_right, size: 20, color: palette.secondaryText),
            ],
          ),
        ),
      ),
    );
  }
}
