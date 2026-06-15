// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'progress_overview_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(progressRepository)
const progressRepositoryProvider = ProgressRepositoryProvider._();

final class ProgressRepositoryProvider
    extends
        $FunctionalProvider<
          ProgressRepository,
          ProgressRepository,
          ProgressRepository
        >
    with $Provider<ProgressRepository> {
  const ProgressRepositoryProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'progressRepositoryProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$progressRepositoryHash();

  @$internal
  @override
  $ProviderElement<ProgressRepository> $createElement(
    $ProviderPointer pointer,
  ) => $ProviderElement(pointer);

  @override
  ProgressRepository create(Ref ref) {
    return progressRepository(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(ProgressRepository value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<ProgressRepository>(value),
    );
  }
}

String _$progressRepositoryHash() =>
    r'39f5120c65863a3a66622d678d7871dd1bd30f97';

@ProviderFor(progressLocalStorage)
const progressLocalStorageProvider = ProgressLocalStorageProvider._();

final class ProgressLocalStorageProvider
    extends $FunctionalProvider<LocalStorage, LocalStorage, LocalStorage>
    with $Provider<LocalStorage> {
  const ProgressLocalStorageProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'progressLocalStorageProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$progressLocalStorageHash();

  @$internal
  @override
  $ProviderElement<LocalStorage> $createElement($ProviderPointer pointer) =>
      $ProviderElement(pointer);

  @override
  LocalStorage create(Ref ref) {
    return progressLocalStorage(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(LocalStorage value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<LocalStorage>(value),
    );
  }
}

String _$progressLocalStorageHash() =>
    r'67c3d233b7010d49e78541203343188869d088cd';

@ProviderFor(ProgressOverviewNotifier)
const progressOverviewProvider = ProgressOverviewNotifierProvider._();

final class ProgressOverviewNotifierProvider
    extends $NotifierProvider<ProgressOverviewNotifier, ProgressOverviewState> {
  const ProgressOverviewNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'progressOverviewProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$progressOverviewNotifierHash();

  @$internal
  @override
  ProgressOverviewNotifier create() => ProgressOverviewNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(ProgressOverviewState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<ProgressOverviewState>(value),
    );
  }
}

String _$progressOverviewNotifierHash() =>
    r'04d89e6b1b7ad86b96ff9f5a6fdc898f8878a883';

abstract class _$ProgressOverviewNotifier
    extends $Notifier<ProgressOverviewState> {
  ProgressOverviewState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<ProgressOverviewState, ProgressOverviewState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<ProgressOverviewState, ProgressOverviewState>,
              ProgressOverviewState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
