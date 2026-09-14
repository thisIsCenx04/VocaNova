import 'dart:async';

import 'package:audioplayers/audioplayers.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';

class MockPlayer extends Mock implements AudioPlayer {}

class MockTts extends Mock implements FlutterTts {}

void main() {
  late MockPlayer player;
  late MockTts tts;
  late AudioPlaybackService service;

  setUpAll(
    () => registerFallbackValue(UrlSource('https://example.test/a.mp3')),
  );
  setUp(() {
    player = MockPlayer();
    tts = MockTts();
    when(() => player.stop()).thenAnswer((_) async {});
    when(() => player.dispose()).thenAnswer((_) async {});
    when(() => player.play(any())).thenAnswer((_) async {});
    when(() => tts.stop()).thenAnswer((_) async => 1);
    when(() => tts.setLanguage(any())).thenAnswer((_) async => 1);
    when(() => tts.setSpeechRate(any())).thenAnswer((_) async => 1);
    when(() => tts.setPitch(any())).thenAnswer((_) async => 1);
    when(() => tts.speak(any())).thenAnswer((_) async => 1);
    service = AudioPlaybackService(player: player, textToSpeech: tts);
  });
  tearDown(() => service.dispose());

  test(
    'plays the supplied recording without the diagnostic sentence',
    () async {
      await service.playPronunciation(
        word: 'run',
        accent: 'UK',
        audioUrl: ' https://example.test/run.mp3 ',
      );
      final source =
          verify(() => player.play(captureAny())).captured.single as UrlSource;
      expect(source.url, 'https://example.test/run.mp3');
      verifyNever(() => tts.speak(any()));
    },
  );

  test(
    'missing and invalid URLs speak the actual word in the selected accent',
    () async {
      for (final url in [null, '', 'file:///tmp/a.mp3', 'https:///']) {
        await service.playPronunciation(
          word: 'run',
          accent: ' us ',
          audioUrl: url,
        );
      }
      verify(() => tts.setLanguage('en-US')).called(4);
      verify(() => tts.speak('run')).called(4);
      verifyNever(() => player.play(any()));
    },
  );

  test('failed recording falls back to UK TTS', () async {
    when(
      () => player.play(any()),
    ).thenThrow(PlatformException(code: 'unavailable'));
    await service.playPronunciation(
      word: 'walk',
      accent: 'UK',
      audioUrl: 'https://example.test/missing.mp3',
    );
    verify(() => tts.setLanguage('en-GB')).called(1);
    verify(() => tts.speak('walk')).called(1);
  });

  test('TTS failure reaches the UI and does not poison the next tap', () async {
    when(() => tts.speak('run')).thenAnswer((_) async => 0);
    await expectLater(
      service.playPronunciation(word: 'run', accent: 'UK'),
      throwsStateError,
    );
    await service.playPronunciation(word: 'walk', accent: 'US');
    verify(() => tts.speak('walk')).called(1);
  });

  test(
    'a superseded failed recording does not speak the previous word',
    () async {
      final loading = Completer<void>();
      final started = Completer<void>();
      when(() => player.play(any())).thenAnswer((_) {
        started.complete();
        return loading.future;
      });
      final old = service.playPronunciation(
        word: 'old',
        accent: 'UK',
        audioUrl: 'https://example.test/old.mp3',
      );
      await started.future;
      final latest = service.playPronunciation(word: 'latest', accent: 'US');
      loading.completeError(PlatformException(code: 'unavailable'));
      await Future.wait([old, latest]);
      verifyNever(() => tts.speak('old'));
      verify(() => tts.speak('latest')).called(1);
    },
  );
}
