// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'lists_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(listsRepository)
const listsRepositoryProvider = ListsRepositoryProvider._();

final class ListsRepositoryProvider
    extends
        $FunctionalProvider<ListsRepository, ListsRepository, ListsRepository>
    with $Provider<ListsRepository> {
  const ListsRepositoryProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'listsRepositoryProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$listsRepositoryHash();

  @$internal
  @override
  $ProviderElement<ListsRepository> $createElement($ProviderPointer pointer) =>
      $ProviderElement(pointer);

  @override
  ListsRepository create(Ref ref) {
    return listsRepository(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(ListsRepository value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<ListsRepository>(value),
    );
  }
}

String _$listsRepositoryHash() => r'7bc591c49dcd294de21a97bc5247b2f8419040df';

@ProviderFor(listsLocalStorage)
const listsLocalStorageProvider = ListsLocalStorageProvider._();

final class ListsLocalStorageProvider
    extends $FunctionalProvider<LocalStorage, LocalStorage, LocalStorage>
    with $Provider<LocalStorage> {
  const ListsLocalStorageProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'listsLocalStorageProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$listsLocalStorageHash();

  @$internal
  @override
  $ProviderElement<LocalStorage> $createElement($ProviderPointer pointer) =>
      $ProviderElement(pointer);

  @override
  LocalStorage create(Ref ref) {
    return listsLocalStorage(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(LocalStorage value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<LocalStorage>(value),
    );
  }
}

String _$listsLocalStorageHash() => r'6de389ad4c377ee0bb4c3770ccb7bcde3a487eac';

@ProviderFor(ListsNotifier)
const listsProvider = ListsNotifierProvider._();

final class ListsNotifierProvider
    extends $NotifierProvider<ListsNotifier, ListsState> {
  const ListsNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'listsProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$listsNotifierHash();

  @$internal
  @override
  ListsNotifier create() => ListsNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(ListsState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<ListsState>(value),
    );
  }
}

String _$listsNotifierHash() => r'5d225e8fd05b24e74af79a5ae4474bdb413a3bd8';

abstract class _$ListsNotifier extends $Notifier<ListsState> {
  ListsState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<ListsState, ListsState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<ListsState, ListsState>,
              ListsState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
