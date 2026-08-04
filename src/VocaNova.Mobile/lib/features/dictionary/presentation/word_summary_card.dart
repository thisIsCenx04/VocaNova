import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

class WordSummaryCard extends StatelessWidget {
  const WordSummaryCard({
    required this.word,
    required this.onTap,
    this.alternate = false,
    super.key,
  });

  final WordSummary word;
  final VoidCallback onTap;

  /// Dòng lẻ đổi nền nhạt hơn để hai dòng liền nhau dễ phân biệt khi lướt.
  final bool alternate;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final baseSurface = dark ? AppColors.surface : Colors.white;
    final text = dark ? AppColors.onSurface : AppColors.lightOnSurface;
    final surface = alternate
        ? Color.alphaBlend(text.withValues(alpha: 0.07), baseSurface)
        : baseSurface;
    final secondary = dark ? const Color(0xFFAAA6B3) : AppColors.lightLabel;
    final border = dark ? const Color(0xFF3A3A3A) : AppColors.lightBorder;
    return Material(
      color: surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: border, width: 1.25),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        key: Key('word-card-${word.wordId}'),
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 13, 12, 13),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Flexible(
                          child: Text(
                            word.word,
                            style: AppTextStyles.button.copyWith(
                              color: text,
                              fontSize: 17,
                              height: 23.8 / 17,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        if (word.cefr != null) ...[
                          const SizedBox(width: 10),
                          _CefrBadge(level: word.cefr!),
                        ],
                      ],
                    ),
                    if (word.phonetic != null) ...[
                      const SizedBox(height: 4),
                      Text(
                        word.phonetic!,
                        style: AppTextStyles.caption.copyWith(
                          color: AppColors.primary,
                          fontSize: 13,
                        ),
                      ),
                    ],
                    if (word.primaryMeaning != null) ...[
                      const SizedBox(height: 8),
                      Text(
                        word.primaryMeaning!,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: AppTextStyles.caption.copyWith(
                          color: secondary,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Icon(Icons.chevron_right, size: 18, color: secondary),
            ],
          ),
        ),
      ),
    );
  }
}

class _CefrBadge extends StatelessWidget {
  const _CefrBadge({required this.level});

  final String level;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: AppColors.primary.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        child: Text(
          level,
          style: AppTextStyles.caption.copyWith(
            color: AppColors.primary,
            fontSize: 11,
            height: 16.5 / 11,
            fontWeight: FontWeight.w800,
          ),
        ),
      ),
    );
  }
}
