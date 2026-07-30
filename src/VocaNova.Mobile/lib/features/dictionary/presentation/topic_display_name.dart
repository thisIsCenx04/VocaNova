import 'package:flutter/widgets.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

extension TopicDisplayName on TopicSummary {
  /// Picks [name] or [nameVi] based on the current app locale, instead of
  /// always preferring the Vietnamese name regardless of language setting.
  String localizedName(BuildContext context) {
    final isVietnamese = Localizations.localeOf(context).languageCode == 'vi';
    if (isVietnamese && nameVi?.trim().isNotEmpty == true) {
      return nameVi!;
    }
    return name;
  }
}
