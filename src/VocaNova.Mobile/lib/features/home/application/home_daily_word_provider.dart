import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';

final homeDailyWordProvider = FutureProvider.autoDispose<WordDetail>((ref) {
  return ref.read(wordDetailRepositoryProvider).getDailyWord();
});
