import 'package:audioplayers/audioplayers.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

part 'audio_playback_service.g.dart';

class AudioPlaybackService {
  AudioPlaybackService()
    : _player = AudioPlayer(),
      _textToSpeech = FlutterTts();

  final AudioPlayer _player;
  final FlutterTts _textToSpeech;

  /// Temporary diagnostic mode: every pronunciation button plays the same
  /// sentence so audio output can be verified independently of dictionary data.
  static const bool useTestVoice = true;
  static const String testVoiceText =
      'Hello. This is the VocaNova pronunciation test.';

  Future<void> playPronunciation({
    required String word,
    required String accent,
    String? audioUrl,
  }) async {
    if (useTestVoice) {
      await speak(testVoiceText, accent: accent);
      return;
    }
    if (audioUrl?.trim().isNotEmpty == true) {
      await play(audioUrl!);
      return;
    }
    await speak(word, accent: accent);
  }

  Future<void> play(String url) async {
    final uri = Uri.tryParse(url);
    if (uri == null || !uri.hasScheme) {
      throw const FormatException('Invalid audio URL.');
    }
    await _textToSpeech.stop();
    await _player.stop();
    await _player.play(UrlSource(uri.toString()));
  }

  Future<void> speak(String word, {String accent = 'UK'}) async {
    await _player.stop();
    await _textToSpeech.stop();
    await _textToSpeech.setLanguage(
      accent.toUpperCase() == 'US' ? 'en-US' : 'en-GB',
    );
    await _textToSpeech.setSpeechRate(0.42);
    await _textToSpeech.setPitch(1);
    final result = await _textToSpeech.speak(word);
    if (result != 1) {
      throw StateError('Text-to-speech could not start.');
    }
  }

  Future<void> dispose() async {
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
