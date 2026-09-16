import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
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
          _InlineWordVideo(
            video: video,
            stopAudio: ref.read(audioPlaybackServiceProvider).stop,
          ),
        ],
      ),
    );
  }
}

class _InlineWordVideo extends StatefulWidget {
  const _InlineWordVideo({required this.video, required this.stopAudio});

  final WordVideo video;
  final Future<void> Function() stopAudio;

  @override
  State<_InlineWordVideo> createState() => _InlineWordVideoState();
}

class _InlineWordVideoState extends State<_InlineWordVideo>
    with WidgetsBindingObserver {
  VideoPlayerController? _controller;
  bool _loading = false;
  bool _failed = false;
  bool _foreground = true;
  bool _controlsVisible = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
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
      if (_foreground) {
        await controller.play();
        if (mounted) setState(() => _controlsVisible = false);
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _loading = false;
          _failed = true;
        });
      }
    }
  }

  Future<void> _toggle() async {
    try {
      final controller = _controller;
      if (controller == null || !controller.value.isInitialized || _failed) {
        await _load();
        return;
      }
      if (controller.value.isPlaying) {
        await controller.pause();
        if (mounted) setState(() => _controlsVisible = true);
      } else {
        await widget.stopAudio();
        if (controller.value.isCompleted) {
          await controller.seekTo(Duration.zero);
        }
        if (mounted && _foreground) {
          await controller.play();
          if (mounted) setState(() => _controlsVisible = false);
        }
      }
    } catch (_) {
      if (mounted) setState(() => _failed = true);
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
    final ready = controller != null && controller.value.isInitialized;
    final isPlaying = ready && controller.value.isPlaying;
    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: AspectRatio(
        aspectRatio: ready ? controller.value.aspectRatio : 16 / 9,
        child: GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTap: ready && isPlaying
              ? () => setState(() => _controlsVisible = !_controlsVisible)
              : null,
          child: Stack(
            fit: StackFit.expand,
            children: [
              if (ready)
                VideoPlayer(controller)
              else
                CachedNetworkImage(
                  imageUrl: widget.video.thumbnailUrl,
                  fit: BoxFit.contain,
                  errorWidget: (_, _, _) =>
                      const ColoredBox(color: Colors.black12),
                ),
              if (_loading)
                const ColoredBox(
                  color: Colors.black26,
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (_failed)
                ColoredBox(
                  color: Colors.black45,
                  child: Center(
                    child: TextButton(
                      onPressed: _load,
                      child: Text(l10n.dictVideoRetry),
                    ),
                  ),
                )
              else if (!isPlaying || _controlsVisible)
                Center(
                  child: IconButton(
                    key: const Key('word-video-play'),
                    tooltip: isPlaying
                        ? l10n.dictVideoPause
                        : l10n.dictVideoPlay,
                    style: IconButton.styleFrom(
                      backgroundColor: Theme.of(
                        context,
                      ).colorScheme.primary.withValues(alpha: 0.9),
                      foregroundColor: Colors.white,
                    ),
                    iconSize: 40,
                    icon: Icon(isPlaying ? Icons.pause : Icons.play_arrow),
                    onPressed: _toggle,
                  ),
                ),
              if (ready)
                Positioned(
                  left: 0,
                  right: 0,
                  bottom: 0,
                  child: VideoProgressIndicator(
                    controller,
                    allowScrubbing: true,
                    padding: EdgeInsets.zero,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
