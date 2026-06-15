// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'quiz_config_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(quizRepository)
const quizRepositoryProvider = QuizRepositoryProvider._();

final class QuizRepositoryProvider
    extends $FunctionalProvider<QuizRepository, QuizRepository, QuizRepository>
    with $Provider<QuizRepository> {
  const QuizRepositoryProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'quizRepositoryProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$quizRepositoryHash();

  @$internal
  @override
  $ProviderElement<QuizRepository> $createElement($ProviderPointer pointer) =>
      $ProviderElement(pointer);

  @override
  QuizRepository create(Ref ref) {
    return quizRepository(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(QuizRepository value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<QuizRepository>(value),
    );
  }
}

String _$quizRepositoryHash() => r'767d479798839599a608018f2bb63676d11a60ea';

@ProviderFor(QuizConfigNotifier)
const quizConfigProvider = QuizConfigNotifierProvider._();

final class QuizConfigNotifierProvider
    extends $NotifierProvider<QuizConfigNotifier, QuizConfigState> {
  const QuizConfigNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'quizConfigProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$quizConfigNotifierHash();

  @$internal
  @override
  QuizConfigNotifier create() => QuizConfigNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(QuizConfigState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<QuizConfigState>(value),
    );
  }
}

String _$quizConfigNotifierHash() =>
    r'4d32ab70f4cd86fad5969c430bd7d7b2e9216c80';

abstract class _$QuizConfigNotifier extends $Notifier<QuizConfigState> {
  QuizConfigState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<QuizConfigState, QuizConfigState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<QuizConfigState, QuizConfigState>,
              QuizConfigState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
