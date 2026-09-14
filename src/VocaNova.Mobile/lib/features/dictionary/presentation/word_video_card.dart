import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:video_player/video_player.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_detail.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class WordVideoCard extends ConsumerWidget {
  const WordVideoCard({required this.video, super.key});
  final WordVideo video;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    return Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.dictVideoTitle,
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: AspectRatio(
              aspectRatio: 16 / 9,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  CachedNetworkImage(
                    imageUrl: video.thumbnailUrl,
                    fit: BoxFit.contain,
                    errorWidget: (_, _, _) =>
                        const ColoredBox(color: Colors.black12),
                  ),
                  Center(
                    child: IconButton.filled(
                      key: const Key('word-video-play'),
                      tooltip: l10n.dictVideoPlay,
                      iconSize: 40,
                      icon: const Icon(Icons.play_arrow),
                      onPressed: () {
                        final audio = ref.read(audioPlaybackServiceProvider);
                        showDialog<void>(
                          context: context,
                          builder: (_) => WordVideoDialog(
                            video: video,
                            stopAudio: audio.stop,
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class WordVideoDialog extends StatefulWidget {
  const WordVideoDialog({
    required this.video,
    required this.stopAudio,
    super.key,
  });
  final WordVideo video;
  final Future<void> Function() stopAudio;

  @override
  State<WordVideoDialog> createState() => _WordVideoDialogState();
}

class _WordVideoDialogState extends State<WordVideoDialog>
    with WidgetsBindingObserver {
  VideoPlayerController? _controller;
  bool _loading = true;
  bool _failed = false;
  bool _foreground = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _failed = false;
    });
    VideoPlayerController? controller;
    try {
      final previous = _controller;
      _controller = null;
      await previous?.dispose();
      await widget.stopAudio();
      if (!mounted) return;
      final uri = Uri.parse(widget.video.url);
      if (uri.scheme != 'https' || uri.host.isEmpty) {
        throw const FormatException('Invalid video URL');
      }
      controller = VideoPlayerController.networkUrl(
        uri,
        viewType: !kIsWeb && defaultTargetPlatform == TargetPlatform.android
            ? VideoViewType.platformView
            : VideoViewType.textureView,
      );
      _controller = controller;
      controller.addListener(_changed);
      await controller.initialize().timeout(const Duration(seconds: 30));
      if (!mounted || _controller != controller) return;
      setState(() => _loading = false);
      if (_foreground) await controller.play();
    } catch (_) {
      if (mounted) {
        setState(() {
          _loading = false;
          _failed = true;
        });
      }
    }
  }

  void _changed() {
    if (mounted) {
      setState(
        () => _failed = _failed || (_controller?.value.hasError ?? false),
      );
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _foreground = state == AppLifecycleState.resumed;
    if (!_foreground) _controller?.pause();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _controller?.removeListener(_changed);
    _controller?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final controller = _controller;
    return Dialog(
      child: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Expanded(child: Text(l10n.dictVideoTitle)),
                  IconButton(
                    tooltip: l10n.dictVideoClose,
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.close),
                  ),
                ],
              ),
              if (_loading)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: CircularProgressIndicator(),
                )
              else if (_failed) ...[
                Text(l10n.dictVideoError),
                TextButton(onPressed: _load, child: Text(l10n.dictVideoRetry)),
              ] else if (controller != null &&
                  controller.value.isInitialized) ...[
                AspectRatio(
                  aspectRatio: controller.value.aspectRatio,
                  child: VideoPlayer(controller),
                ),
                VideoProgressIndicator(controller, allowScrubbing: true),
                IconButton(
                  tooltip: controller.value.isPlaying
                      ? l10n.dictVideoPause
                      : l10n.dictVideoPlay,
                  icon: Icon(
                    controller.value.isPlaying ? Icons.pause : Icons.play_arrow,
                  ),
                  onPressed: () async {
                    try {
                      if (controller.value.isPlaying) {
                        await controller.pause();
                      } else {
                        await widget.stopAudio();
                        if (mounted && _foreground) await controller.play();
                      }
                    } catch (_) {
                      if (mounted) setState(() => _failed = true);
                    }
                  },
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
