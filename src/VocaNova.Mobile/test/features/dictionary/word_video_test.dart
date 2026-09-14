import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:video_player_platform_interface/video_player_platform_interface.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/features/dictionary/data/dtos/word_detail_dto.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_video_card.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class MockAudio extends Mock implements AudioPlaybackService {}

class TestVideoPlatform extends VideoPlayerPlatform {
  final calls = <String>[];
  final streams = <int, StreamController<VideoEvent>>{};
  bool fail = false;

  @override
  Future<void> init() async {}
  @override
  Future<int?> createWithOptions(VideoCreationOptions options) async {
    calls.add('create');
    final id = streams.length;
    final stream = StreamController<VideoEvent>(
      onCancel: () {
        calls.add('cancel-stream');
      },
    );
    streams[id] = stream;
    if (fail) {
      stream.addError(
        PlatformException(
          code: 'unavailable',
          message: 'Unavailable test video',
        ),
      );
    } else {
      stream.add(
        VideoEvent(
          eventType: VideoEventType.initialized,
          size: const Size(320, 180),
          duration: const Duration(seconds: 6),
        ),
      );
    }
    return id;
  }

  @override
  Stream<VideoEvent> videoEventsFor(int playerId) => streams[playerId]!.stream;
  @override
  Future<void> dispose(int playerId) async {
    calls.add('dispose');
    await streams[playerId]!.close();
  }

  @override
  Future<void> play(int playerId) async {
    calls.add('play');
  }

  @override
  Future<void> pause(int playerId) async {
    calls.add('pause');
  }

  @override
  Future<void> setLooping(int playerId, bool looping) async {}
  @override
  Future<void> setVolume(int playerId, double volume) async {}
  @override
  Future<void> setPlaybackSpeed(int playerId, double speed) async {}
  @override
  Future<Duration> getPosition(int playerId) async => Duration.zero;
  @override
  Future<void> seekTo(int playerId, Duration position) async {}
  @override
  Widget buildViewWithOptions(VideoViewOptions options) =>
      const ColoredBox(color: Colors.black);
}

void main() {
  const video = WordVideo(
    videoId: 3,
    url: 'https://example.test/swim.mp4',
    thumbnailUrl: 'https://example.test/swim.jpg',
  );
  late TestVideoPlatform platform;
  late MockAudio audio;
  setUp(() {
    platform = TestVideoPlatform();
    VideoPlayerPlatform.instance = platform;
    audio = MockAudio();
    when(() => audio.stop()).thenAnswer((_) async {
      platform.calls.add('stop-audio');
    });
  });

  Widget app() => ProviderScope(
    overrides: [audioPlaybackServiceProvider.overrideWithValue(audio)],
    child: MaterialApp(
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      supportedLocales: AppLocalizations.supportedLocales,
      locale: const Locale('en'),
      home: const Scaffold(
        body: SingleChildScrollView(child: WordVideoCard(video: video)),
      ),
    ),
  );

  test('wire model preserves video through local cache serialization', () {
    final dto = WordVideoDto.fromJson({
      'video_id': 3,
      'url': video.url,
      'thumbnail_url': video.thumbnailUrl,
    });
    expect(dto.toDomain().url, video.url);
    expect(
      WordVideoDto.fromDomain(dto.toDomain()).toJson()['thumbnail_url'],
      video.thumbnailUrl,
    );
  });

  testWidgets(
    'loads only on tap, stops audio first, pauses in background, disposes on close',
    (tester) async {
      await tester.pumpWidget(app());
      await tester.pumpAndSettle();
      expect(platform.calls, isEmpty);
      await tester.tap(find.byKey(const Key('word-video-play')));
      await tester.pumpAndSettle();
      expect(
        platform.calls.indexOf('stop-audio'),
        lessThan(platform.calls.indexOf('create')),
      );
      expect(platform.calls, contains('play'));
      platform.calls.clear();
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
      await tester.pumpAndSettle();
      expect(platform.calls, contains('pause'));
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
      await tester.pump();
      await tester.tap(find.byIcon(Icons.close));
      await tester.pumpAndSettle();
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle();
      expect(find.byType(WordVideoDialog, skipOffstage: false), findsNothing);
      await tester.runAsync(() async {
        await Future<void>.delayed(Duration.zero);
      });
      expect(platform.calls, contains('dispose'));
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'failed initialization shows retry and replaces failed controller',
    (tester) async {
      platform.fail = true;
      await tester.pumpWidget(app());
      await tester.tap(find.byKey(const Key('word-video-play')));
      await tester.pumpAndSettle();
      expect(find.text('Try again'), findsOneWidget);
      platform.fail = false;
      await tester.tap(find.text('Try again'));
      await tester.runAsync(() async {
        await Future<void>.delayed(Duration.zero);
      });
      await tester.pumpAndSettle();
      expect(platform.calls.where((call) => call == 'create').length, 2);
      await tester.runAsync(() async {
        await Future<void>.delayed(Duration.zero);
      });
      expect(platform.calls, contains('dispose'));
      expect(platform.calls, contains('play'));
      await tester.tap(find.byIcon(Icons.close));
      await tester.pumpAndSettle();
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle();
      expect(find.byType(WordVideoDialog, skipOffstage: false), findsNothing);
    },
  );
}
