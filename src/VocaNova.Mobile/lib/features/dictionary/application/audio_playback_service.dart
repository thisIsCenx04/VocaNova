import 'package:audioplayers/audioplayers.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

part 'audio_playback_service.g.dart';

class AudioPlaybackService {
  AudioPlaybackService({AudioPlayer? player, FlutterTts? textToSpeech})
    : _player = player ?? AudioPlayer(),
      _textToSpeech = textToSpeech ?? FlutterTts();

  final AudioPlayer _player;
  final FlutterTts _textToSpeech;

  Future<void> _pending = Future.value();
  int _request = 0;
  bool _disposed = false;

  Future<void> playPronunciation({
    required String word,
    required String accent,
    String? audioUrl,
  }) {
    final request = ++_request;
    final operation = _pending.then((_) async {
      if (_disposed || request != _request) return;
      if (audioUrl?.trim().isNotEmpty == true) {
        try {
          await play(audioUrl!.trim());
          return;
        } on Exception {
          // An unavailable recording must not prevent pronunciation via TTS.
        }
      }
      if (_disposed || request != _request) return;
      await speak(word.trim(), accent: accent);
    });
    // Keep later taps usable even if both the recording and TTS fail.
    _pending = operation.then<void>(
      (_) {},
      onError: (Object _, StackTrace _) {},
    );
    return operation;
  }

  Future<void> play(String url) async {
    final uri = Uri.tryParse(url);
    if (uri == null ||
        !['https', 'http'].contains(uri.scheme) ||
        uri.host.isEmpty) {
      throw const FormatException('Invalid audio URL.');
    }
    await _textToSpeech.stop();
    await _player.stop();
    await _player.play(UrlSource(uri.toString()));
  }

  Future<void> speak(String word, {String accent = 'UK'}) async {
    if (word.isEmpty) throw const FormatException('Word is empty.');
    await _player.stop();
    await _textToSpeech.stop();
    await _textToSpeech.setLanguage(
      accent.trim().toUpperCase() == 'US' ? 'en-US' : 'en-GB',
    );
    await _textToSpeech.setSpeechRate(0.42);
    await _textToSpeech.setPitch(1);
    final result = await _textToSpeech.speak(word);
    if (result != 1) {
      throw StateError('Text-to-speech could not start.');
    }
  }

  Future<void> stop() async {
    _request++;
    await _player.stop();
    await _textToSpeech.stop();
    // Wait for an in-flight source preparation so it cannot start behind video.
    await _pending;
    await _player.stop();
    await _textToSpeech.stop();
  }

  Future<void> dispose() async {
    _disposed = true;
    _request++;
    await _pending;
    await _player.dispose();
    await _textToSpeech.stop();
  }
}

@Riverpod(keepAlive: true)
AudioPlaybackService audioPlaybackService(Ref ref) {
  final service = AudioPlaybackService();
  ref.onDispose(service.dispose);
  return service;
}
