// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'word_detail_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(wordDetailRepository)
const wordDetailRepositoryProvider = WordDetailRepositoryProvider._();

final class WordDetailRepositoryProvider
    extends
        $FunctionalProvider<
          WordDetailRepository,
          WordDetailRepository,
          WordDetailRepository
        >
    with $Provider<WordDetailRepository> {
  const WordDetailRepositoryProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'wordDetailRepositoryProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$wordDetailRepositoryHash();

  @$internal
  @override
  $ProviderElement<WordDetailRepository> $createElement(
    $ProviderPointer pointer,
  ) => $ProviderElement(pointer);

  @override
  WordDetailRepository create(Ref ref) {
    return wordDetailRepository(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(WordDetailRepository value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<WordDetailRepository>(value),
    );
  }
}

String _$wordDetailRepositoryHash() =>
    r'e927ee00982bc4472dca1ac9ce53717361e2d52a';

@ProviderFor(wordDetailLocalStorage)
const wordDetailLocalStorageProvider = WordDetailLocalStorageProvider._();

final class WordDetailLocalStorageProvider
    extends $FunctionalProvider<LocalStorage, LocalStorage, LocalStorage>
    with $Provider<LocalStorage> {
  const WordDetailLocalStorageProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'wordDetailLocalStorageProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$wordDetailLocalStorageHash();

  @$internal
  @override
  $ProviderElement<LocalStorage> $createElement($ProviderPointer pointer) =>
      $ProviderElement(pointer);

  @override
  LocalStorage create(Ref ref) {
    return wordDetailLocalStorage(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(LocalStorage value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<LocalStorage>(value),
    );
  }
}

String _$wordDetailLocalStorageHash() =>
    r'914380a2ed7fc22fda56d4ab6e4cbca587cf3846';

@ProviderFor(WordDetailNotifier)
const wordDetailProvider = WordDetailNotifierFamily._();

final class WordDetailNotifierProvider
    extends $NotifierProvider<WordDetailNotifier, WordDetailState> {
  const WordDetailNotifierProvider._({
    required WordDetailNotifierFamily super.from,
    required int super.argument,
  }) : super(
         retry: null,
         name: r'wordDetailProvider',
         isAutoDispose: false,
         dependencies: null,
         $allTransitiveDependencies: null,
       );

  @override
  String debugGetCreateSourceHash() => _$wordDetailNotifierHash();

  @override
  String toString() {
    return r'wordDetailProvider'
        ''
        '($argument)';
  }

  @$internal
  @override
  WordDetailNotifier create() => WordDetailNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(WordDetailState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<WordDetailState>(value),
    );
  }

  @override
  bool operator ==(Object other) {
    return other is WordDetailNotifierProvider && other.argument == argument;
  }

  @override
  int get hashCode {
    return argument.hashCode;
  }
}

String _$wordDetailNotifierHash() =>
    r'501df5a9190ff2de29833b1eaf896340dae1dc02';

final class WordDetailNotifierFamily extends $Family
    with
        $ClassFamilyOverride<
          WordDetailNotifier,
          WordDetailState,
          WordDetailState,
          WordDetailState,
          int
        > {
  const WordDetailNotifierFamily._()
    : super(
        retry: null,
        name: r'wordDetailProvider',
        dependencies: null,
        $allTransitiveDependencies: null,
        isAutoDispose: false,
      );

  WordDetailNotifierProvider call(int wordId) =>
      WordDetailNotifierProvider._(argument: wordId, from: this);

  @override
  String toString() => r'wordDetailProvider';
}

abstract class _$WordDetailNotifier extends $Notifier<WordDetailState> {
  late final _$args = ref.$arg as int;
  int get wordId => _$args;

  WordDetailState build(int wordId);
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build(_$args);
    final ref = this.ref as $Ref<WordDetailState, WordDetailState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<WordDetailState, WordDetailState>,
              WordDetailState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
