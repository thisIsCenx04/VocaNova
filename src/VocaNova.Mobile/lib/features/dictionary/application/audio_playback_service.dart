import 'package:audioplayers/audioplayers.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

part 'audio_playback_service.g.dart';

class AudioPlaybackService {
  AudioPlaybackService() : _player = AudioPlayer();

  final AudioPlayer _player;

  Future<void> play(String url) => _player.play(UrlSource(url));

  Future<void> dispose() => _player.dispose();
}

@Riverpod(keepAlive: true)
AudioPlaybackService audioPlaybackService(Ref ref) {
  final service = AudioPlaybackService();
  ref.onDispose(service.dispose);
  return service;
}
