import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';

IconData topicIconData(String? icon, String name) {
  final value = '${icon ?? ''} $name'.toLowerCase().replaceAll(
    RegExp(r'[^a-z0-9]+'),
    ' ',
  );

  bool hasAny(Iterable<String> words) =>
      words.any((word) => value.contains(word));

  if (hasAny(['plane', 'airplane', 'travel', 'tourism', 'trip'])) {
    return Icons.flight_takeoff;
  }
  if (hasAny(['briefcase', 'business', 'work', 'job', 'office', 'career'])) {
    return Icons.business_center_outlined;
  }
  if (hasAny(['education', 'academic', 'school', 'study', 'university'])) {
    return Icons.school_outlined;
  }
  if (hasAny(['finance', 'money', 'bank', 'economy', 'investment'])) {
    return Icons.account_balance_wallet_outlined;
  }
  if (hasAny(['food', 'meal', 'cooking', 'restaurant', 'eat'])) {
    return Icons.restaurant_outlined;
  }
  if (hasAny(['art', 'arts', 'culture', 'music', 'painting'])) {
    return Icons.palette_outlined;
  }
  if (hasAny(['technology', 'tech', 'computer', 'software', 'digital'])) {
    return Icons.devices_outlined;
  }
  if (hasAny(['health', 'medical', 'medicine', 'fitness'])) {
    return Icons.health_and_safety_outlined;
  }
  if (hasAny(['sport', 'sports', 'ball', 'football'])) {
    return Icons.sports_soccer;
  }
  if (hasAny(['science', 'lab', 'research'])) {
    return Icons.science_outlined;
  }
  if (hasAny(['daily', 'life', 'home', 'family'])) {
    return Icons.home_outlined;
  }
  if (hasAny(['conversation', 'speaking', 'communication', 'language'])) {
    return Icons.forum_outlined;
  }
  if (hasAny(['run', 'movement', 'transport'])) {
    return Icons.directions_run;
  }
  return Icons.menu_book_outlined;
}

class TopicIcon extends StatelessWidget {
  const TopicIcon({
    required this.name,
    this.icon,
    this.size = 30,
    this.color,
    super.key,
  });

  final String name;
  final String? icon;
  final double size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Icon(
      topicIconData(icon, name),
      size: size,
      color: color ?? AppColors.primary,
    );
  }
}
