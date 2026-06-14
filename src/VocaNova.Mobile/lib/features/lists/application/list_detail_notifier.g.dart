// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'list_detail_notifier.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(ListDetailNotifier)
const listDetailProvider = ListDetailNotifierFamily._();

final class ListDetailNotifierProvider
    extends $NotifierProvider<ListDetailNotifier, ListDetailState> {
  const ListDetailNotifierProvider._({
    required ListDetailNotifierFamily super.from,
    required int super.argument,
  }) : super(
         retry: null,
         name: r'listDetailProvider',
         isAutoDispose: false,
         dependencies: null,
         $allTransitiveDependencies: null,
       );

  @override
  String debugGetCreateSourceHash() => _$listDetailNotifierHash();

  @override
  String toString() {
    return r'listDetailProvider'
        ''
        '($argument)';
  }

  @$internal
  @override
  ListDetailNotifier create() => ListDetailNotifier();

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(ListDetailState value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<ListDetailState>(value),
    );
  }

  @override
  bool operator ==(Object other) {
    return other is ListDetailNotifierProvider && other.argument == argument;
  }

  @override
  int get hashCode {
    return argument.hashCode;
  }
}

String _$listDetailNotifierHash() =>
    r'3ce55e0b726c8d16d0273490f918916cc18d6dd2';

final class ListDetailNotifierFamily extends $Family
    with
        $ClassFamilyOverride<
          ListDetailNotifier,
          ListDetailState,
          ListDetailState,
          ListDetailState,
          int
        > {
  const ListDetailNotifierFamily._()
    : super(
        retry: null,
        name: r'listDetailProvider',
        dependencies: null,
        $allTransitiveDependencies: null,
        isAutoDispose: false,
      );

  ListDetailNotifierProvider call(int listId) =>
      ListDetailNotifierProvider._(argument: listId, from: this);

  @override
  String toString() => r'listDetailProvider';
}

abstract class _$ListDetailNotifier extends $Notifier<ListDetailState> {
  late final _$args = ref.$arg as int;
  int get listId => _$args;

  ListDetailState build(int listId);
  @$mustCallSuper
  @override
  void runBuild() {
    final created = build(_$args);
    final ref = this.ref as $Ref<ListDetailState, ListDetailState>;
    final element =
        ref.element
            as $ClassProviderElement<
              AnyNotifier<ListDetailState, ListDetailState>,
              ListDetailState,
              Object?,
              Object?
            >;
    element.handleValue(ref, created);
  }
}
