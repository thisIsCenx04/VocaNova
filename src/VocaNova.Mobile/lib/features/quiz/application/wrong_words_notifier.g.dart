// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'wrong_words_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(WrongWordsNotifier)
const wrongWordsProvider = WrongWordsNotifierProvider._();

final class WrongWordsNotifierProvider
    extends $NotifierProvider<WrongWordsNotifier, WrongWordsState> {
  const WrongWordsNotifierProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'wrongWordsProvider',
        isAutoDispose: true,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$wrongWordsNotifierHash();

  @$internal
  @override
  WrongWordsNotifier create() => WrongWordsNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(WrongWordsState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<WrongWordsState>(value),
    );
  }
}

String _$wrongWordsNotifierHash() =>
    r'3c0ac56b876438519cfafa1e8cd4455d6c6b42fa';

abstract class _$WrongWordsNotifier extends $Notifier<WrongWordsState> {
  WrongWordsState build();
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build();
    final ref = this.ref as $Ref<WrongWordsState, WrongWordsState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<WrongWordsState, WrongWordsState>,
              WrongWordsState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
