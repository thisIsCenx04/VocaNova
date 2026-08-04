// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'audio_playback_service.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint, type=warning

@ProviderFor(audioPlaybackService)
const audioPlaybackServiceProvider = AudioPlaybackServiceProvider._();

final class AudioPlaybackServiceProvider
    extends
        $FunctionalProvider<
          AudioPlaybackService,
          AudioPlaybackService,
          AudioPlaybackService
        >
    with $Provider<AudioPlaybackService> {
  const AudioPlaybackServiceProvider._()
    : super(
        from: null,
        argument: null,
        retry: null,
        name: r'audioPlaybackServiceProvider',
        isAutoDispose: false,
        dependencies: null,
        $allTransitiveDependencies: null,
      );

  @override
  String debugGetCreateSourceHash() => _$audioPlaybackServiceHash();

  @$internal
  @override
  $ProviderElement<AudioPlaybackService> $createElement(
    $ProviderPointer pointer,
  ) => $ProviderElement(pointer);

  @override
  AudioPlaybackService create(Ref ref) {
    return audioPlaybackService(ref);
  }

  /// {@macro riverpod.override_with_value}
  Override overrideWithValue(AudioPlaybackService value) {
    return $ProviderOverride(
      origin: this,
      providerOverride: $SyncValueProvider<AudioPlaybackService>(value),
    );
  }
}

String _$audioPlaybackServiceHash() =>
    r'3eabdf4c8ecfba39897ee1907a8af4d184c082de';
