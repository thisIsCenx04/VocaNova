// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'progress_charts_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(ProgressChartsNotifier)
const progressChartsProvider = ProgressChartsNotifierProvider._();

final class ProgressChartsNotifierProvider
    extends $NotifierProvider<ProgressChartsNotifier, ProgressChartsState> {
  const ProgressChartsNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'progressChartsProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$progressChartsNotifierHash();

  @$internal
  @override
  ProgressChartsNotifier create() => ProgressChartsNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(ProgressChartsState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<ProgressChartsState>(value),
    );
  }
}

String _$progressChartsNotifierHash() =>
    r'f7fc42feed6ab83341597ee9794f8ca0f5523ef5';

abstract class _$ProgressChartsNotifier extends $Notifier<ProgressChartsState> {
  ProgressChartsState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<ProgressChartsState, ProgressChartsState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<ProgressChartsState, ProgressChartsState>,
              ProgressChartsState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
