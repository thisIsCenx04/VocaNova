import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

/// Loads the public topic catalogue for the Home "Topics for you" section.
/// Kept separate from the search flow so the Home screen can show/refresh it
/// independently, with its own loading/error states.
final homeTopicsProvider = FutureProvider.autoDispose<List<TopicSummary>>((
  ref,
) async {
  final topics = await ref.read(wordSearchRepositoryProvider).getTopics();
  return topics;
});
