// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'quiz_history_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(QuizHistoryNotifier)
const quizHistoryProvider = QuizHistoryNotifierProvider._();

final class QuizHistoryNotifierProvider
    extends $NotifierProvider<QuizHistoryNotifier, QuizHistoryState> {
  const QuizHistoryNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'quizHistoryProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$quizHistoryNotifierHash();

  @$internal
  @override
  QuizHistoryNotifier create() => QuizHistoryNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(QuizHistoryState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<QuizHistoryState>(value),
    );
  }
}

String _$quizHistoryNotifierHash() =>
    r'2fcdfb6d89e692f7930c91d5b2bc8cda35f4e953';

abstract class _$QuizHistoryNotifier extends $Notifier<QuizHistoryState> {
  QuizHistoryState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<QuizHistoryState, QuizHistoryState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<QuizHistoryState, QuizHistoryState>,
              QuizHistoryState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
