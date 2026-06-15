// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'word_search_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(wordSearchRepository)
const wordSearchRepositoryProvider = WordSearchRepositoryProvider._();

final class WordSearchRepositoryProvider
    extends
        $FunctionalProvider<
          WordSearchRepository,
          WordSearchRepository,
          WordSearchRepository
        >
    with $Provider<WordSearchRepository> {
  const WordSearchRepositoryProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'wordSearchRepositoryProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$wordSearchRepositoryHash();

  @$internal
  @override
  $ProviderElement<WordSearchRepository> $createElement(
    $ProviderPointer pointer,
  ) => $ProviderElement(pointer);

  @override
  WordSearchRepository create(Ref ref) {
    return wordSearchRepository(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(WordSearchRepository value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<WordSearchRepository>(value),
    );
  }
}

String _$wordSearchRepositoryHash() =>
    r'af690cfd6ed1542ae581706fc436e49c8e63222c';

@ProviderFor(searchLocalStorage)
const searchLocalStorageProvider = SearchLocalStorageProvider._();

final class SearchLocalStorageProvider
    extends $FunctionalProvider<LocalStorage, LocalStorage, LocalStorage>
    with $Provider<LocalStorage> {
  const SearchLocalStorageProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'searchLocalStorageProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$searchLocalStorageHash();

  @$internal
  @override
  $ProviderElement<LocalStorage> $createElement($ProviderPointer pointer) =>
      $ProviderElement(pointer);

  @override
  LocalStorage create(Ref ref) {
    return searchLocalStorage(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(LocalStorage value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<LocalStorage>(value),
    );
  }
}

String _$searchLocalStorageHash() =>
    r'8ace460824fe37c82e05e7d66a68ffad265c4221';

@ProviderFor(WordSearchNotifier)
const wordSearchProvider = WordSearchNotifierProvider._();

final class WordSearchNotifierProvider
    extends $NotifierProvider<WordSearchNotifier, WordSearchState> {
  const WordSearchNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'wordSearchProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$wordSearchNotifierHash();

  @$internal
  @override
  WordSearchNotifier create() => WordSearchNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(WordSearchState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<WordSearchState>(value),
    );
  }
}

String _$wordSearchNotifierHash() =>
    r'3c1a0a43571f6c6cd6a14ec2ed374a2df79e1a65';

abstract class _$WordSearchNotifier extends $Notifier<WordSearchState> {
  WordSearchState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<WordSearchState, WordSearchState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<WordSearchState, WordSearchState>,
              WordSearchState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
