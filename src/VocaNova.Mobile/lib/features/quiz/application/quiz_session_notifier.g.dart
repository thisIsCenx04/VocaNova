// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'quiz_session_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(QuizSessionNotifier)
const quizSessionProvider = QuizSessionNotifierFamily._();

final class QuizSessionNotifierProvider
    extends $NotifierProvider<QuizSessionNotifier, QuizSessionState> {
  const QuizSessionNotifierProvider._({
    required QuizSessionNotifierFamily super.from,
    required QuizSessionStart super.argument,
  }) : super(
         retry: null,
         name: r'quizSessionProvider',
         isAutoDispose: true,
         dependencies: null,
         $allTransitiveDependencies: null,
       );

  @override
  String debugGetCreateSourceHash() => _$quizSessionNotifierHash();

  @override
  String toString() {
    return r'quizSessionProvider'
        ''
        '($argument)';
  }

  @$internal
  @override
  QuizSessionNotifier create() => QuizSessionNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(QuizSessionState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<QuizSessionState>(value),
    );
  }

  @override
  bool operator ==(Object other) {
    return other is QuizSessionNotifierProvider && other.argument == argument;
  }

  @override
  int get hashCode {
    return argument.hashCode;
  }
}

String _$quizSessionNotifierHash() =>
    r'861d9858266bcf7c7ca84347f698b1451744c55a';

final class QuizSessionNotifierFamily extends $Family
    with
        $ClassFamilyOverride<
          QuizSessionNotifier,
          QuizSessionState,
          QuizSessionState,
          QuizSessionState,
          QuizSessionStart
        > {
  const QuizSessionNotifierFamily._()
    : super(
        retry: null,
        name: r'quizSessionProvider',
        dependencies: null,
        $allTransitiveDependencies: null,
        isAutoDispose: true,
      );

  QuizSessionNotifierProvider call(QuizSessionStart session) =>
      QuizSessionNotifierProvider._(argument: session, from: this);

  @override
  String toString() => r'quizSessionProvider';
}

abstract class _$QuizSessionNotifier extends $Notifier<QuizSessionState> {
  late final _$args = ref.$arg as QuizSessionStart;
  QuizSessionStart get session => _$args;

  QuizSessionState build(QuizSessionStart session);
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build(_$args);
    final ref = this.ref as $Ref<QuizSessionState, QuizSessionState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<QuizSessionState, QuizSessionState>,
              QuizSessionState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
