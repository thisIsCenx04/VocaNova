import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.lightBackground,
      body: SafeArea(
        bottom: false,
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            _HomeHeader(onNotification: () => context.push(AppRoutes.settings)),
            Padding(
              padding: const EdgeInsets.fromLTRB(25, 17, 25, 10),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Good morning',
                    style: AppTextStyles.caption.copyWith(
                      color: const Color(0xFF888888),
                      fontSize: 13,
                      height: 19.5 / 13,
                    ),
                  ),
                  Text(
                    'Hi, An',
                    style: AppTextStyles.heading.copyWith(
                      color: Colors.black,
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
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HomeHeader extends StatelessWidget {
  const _HomeHeader({required this.onNotification});

  final VoidCallback onNotification;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 56,
      padding: const EdgeInsets.fromLTRB(32, 0, 25, 0),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: Color(0xFFE6E6E6), width: 1)),
      ),
      child: Row(
        children: [
          const _VocaNovaLogo(),
          const SizedBox(width: 9),
          Text(
            'VocaNova',
            style: AppTextStyles.button.copyWith(
              color: Colors.black,
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
  const _VocaNovaLogo();

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 31,
      height: 31,
      child: CustomPaint(painter: _LogoPainter()),
    );
  }
}

class _LogoPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.black
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
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _NotificationButton extends StatelessWidget {
  const _NotificationButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: [
        Material(
          color: const Color(0xFFF7F7F5),
          shape: const CircleBorder(),
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: onPressed,
            child: const SizedBox(
              width: 40,
              height: 40,
              child: Icon(Icons.notifications_none, size: 17),
            ),
          ),
        ),
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
    return Material(
      color: const Color(0xFFF7F7F5),
      borderRadius: BorderRadius.circular(50),
      child: InkWell(
        borderRadius: BorderRadius.circular(50),
        onTap: onTap,
        child: Container(
          height: 44,
          padding: const EdgeInsets.fromLTRB(15, 0, 12, 0),
          decoration: BoxDecoration(
            border: Border.all(color: const Color(0xFFE6E6E6), width: 1.25),
            borderRadius: BorderRadius.circular(50),
          ),
          child: Row(
            children: [
              const Icon(Icons.search, size: 16, color: Color(0xFF888888)),
              const SizedBox(width: 12),
              Text(
                'Search a word...',
                style: AppTextStyles.caption.copyWith(
                  color: const Color(0xFF888888),
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
                  color: const Color(0xFFE6E6E6),
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  '⌘K',
                  style: AppTextStyles.caption.copyWith(
                    color: const Color(0xFF666666),
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

class _DailyGoalCard extends StatelessWidget {
  const _DailyGoalCard();

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minHeight: 142),
      padding: const EdgeInsets.fromLTRB(19, 17, 19, 13),
      decoration: BoxDecoration(
        color: const Color(0xFFDCEEB1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.track_changes, size: 12, color: Colors.black),
              const SizedBox(width: 6),
              Text(
                'DAILY GOAL',
                style: AppTextStyles.caption.copyWith(
                  color: Colors.black,
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
              const _DashedGoalRing(),
              const SizedBox(width: 19),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '18 / 25 words',
                      style: AppTextStyles.button.copyWith(
                        color: Colors.black,
                        fontSize: 18,
                        height: 27 / 18,
                        letterSpacing: -0.3,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    Text(
                      '7 more to keep your streak',
                      style: AppTextStyles.caption.copyWith(
                        color: const Color(0xFF444444),
                        fontSize: 13,
                        height: 19.5 / 13,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        const Icon(Icons.local_fire_department, size: 12),
                        const SizedBox(width: 5),
                        Text(
                          '7-day streak',
                          style: AppTextStyles.caption.copyWith(
                            color: Colors.black,
                            fontSize: 11,
                            height: 16.5 / 11,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const Spacer(),
                        const _StreakDots(),
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
  const _DashedGoalRing();

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 62,
      height: 62,
      child: Stack(
        alignment: Alignment.center,
        children: [
          CustomPaint(
            size: const Size.square(62),
            painter: _DashedRingPainter(progress: 0.72),
          ),
          Text(
            '72%',
            style: AppTextStyles.caption.copyWith(
              color: Colors.black,
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
  const _DashedRingPainter({required this.progress});

  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final rect = Offset.zero & size;
    final basePaint = Paint()
      ..color = Colors.black.withValues(alpha: 0.12)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 4
      ..strokeCap = StrokeCap.round;
    final progressPaint = Paint()
      ..color = Colors.black
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
    return oldDelegate.progress != progress;
  }
}

class _StreakDots extends StatelessWidget {
  const _StreakDots();

  static const days = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        for (var i = 0; i < days.length; i++) ...[
          Column(
            children: [
              Container(
                width: 13,
                height: 13,
                decoration: BoxDecoration(
                  color: i < 5
                      ? Colors.black
                      : Colors.black.withValues(alpha: 0.2),
                  shape: BoxShape.circle,
                ),
                child: i < 5
                    ? const Icon(Icons.check, size: 8, color: Colors.white)
                    : null,
              ),
              const SizedBox(height: 2),
              Text(
                days[i],
                style: AppTextStyles.caption.copyWith(
                  color: Colors.black.withValues(alpha: 0.45),
                  fontSize: 8,
                  height: 12 / 8,
                ),
              ),
            ],
          ),
          if (i != days.length - 1) const SizedBox(width: 4),
        ],
      ],
    );
  }
}

class _WordOfTheDayCard extends StatelessWidget {
  const _WordOfTheDayCard();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 225,
      padding: const EdgeInsets.fromLTRB(19, 18, 19, 17),
      decoration: BoxDecoration(
        color: const Color(0xFFC5B0F4),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'WORD OF THE DAY',
                style: AppTextStyles.caption.copyWith(
                  color: Colors.black,
                  fontSize: 10,
                  height: 15 / 10,
                  letterSpacing: 0.6,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const Spacer(),
              Container(
                width: 32,
                height: 32,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.35),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.volume_up_outlined, size: 16),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(
            'Serendipity',
            style: AppTextStyles.heading.copyWith(
              color: Colors.black,
              fontSize: 28,
              height: 33.6 / 28,
              letterSpacing: -0.9,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            '/ˌser.ənˈdɪp.ə.ti/ · noun',
            style: AppTextStyles.caption.copyWith(
              color: Colors.black,
              fontSize: 13,
              height: 19.5 / 13,
              fontStyle: FontStyle.italic,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            'The occurrence of events by chance in a happy\nor beneficial way.',
            style: AppTextStyles.caption.copyWith(
              color: const Color(0xFF252238),
              fontSize: 13,
              height: 19.5 / 13,
            ),
          ),
          const Spacer(),
          SizedBox(
            width: 135,
            height: 35,
            child: ElevatedButton(
              onPressed: () {},
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.black,
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
              child: const FittedBox(
                fit: BoxFit.scaleDown,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text('Learn this word'),
                    SizedBox(width: 6),
                    Icon(Icons.chevron_right, size: 14),
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

class _StatsRow extends StatelessWidget {
  const _StatsRow();

  @override
  Widget build(BuildContext context) {
    return const Row(
      children: [
        Expanded(
          child: _StatCard(
            icon: Icons.menu_book_outlined,
            iconColor: AppColors.primary,
            value: '248',
            label: 'Words',
          ),
        ),
        SizedBox(width: 10),
        Expanded(
          child: _StatCard(
            icon: Icons.trending_up,
            iconColor: Color(0xFF20C7A3),
            value: '84%',
            label: 'Accuracy',
          ),
        ),
        SizedBox(width: 10),
        Expanded(
          child: _StatCard(
            icon: Icons.emoji_events_outlined,
            iconColor: Color(0xFFFFA425),
            value: '#142',
            label: 'Rank',
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
    return Container(
      height: 105,
      padding: const EdgeInsets.fromLTRB(14, 18, 14, 14),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: const Color(0xFFE6E6E6), width: 1.25),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 26,
            height: 26,
            decoration: const BoxDecoration(
              color: Color(0xFFF7F7F5),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 14, color: iconColor),
          ),
          const Spacer(),
          Text(
            value,
            style: AppTextStyles.heading.copyWith(
              color: Colors.black,
              fontSize: 20,
              height: 24 / 20,
              letterSpacing: -0.3,
            ),
          ),
          Text(
            label,
            style: AppTextStyles.caption.copyWith(
              color: const Color(0xFF888888),
              fontSize: 11,
              height: 16.5 / 11,
            ),
          ),
        ],
      ),
    );
  }
}
