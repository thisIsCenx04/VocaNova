import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_state.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/add_to_list_sheet.dart';

class WordDetailScreen extends ConsumerStatefulWidget {
  const WordDetailScreen({required this.wordId, super.key});

  final int wordId;

  @override
  ConsumerState<WordDetailScreen> createState() => _WordDetailScreenState();
}

class _WordDetailScreenState extends ConsumerState<WordDetailScreen> {
  String _accent = 'UK';
  bool _hasAutoPlayed = false;

  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(wordDetailProvider(widget.wordId).notifier).load(),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(wordDetailProvider(widget.wordId));
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết từ')),
      floatingActionButton: state.word == null
          ? null
          : FloatingActionButton.extended(
              key: const Key('add-to-list-fab'),
              onPressed: () => showModalBottomSheet<void>(
                context: context,
                isScrollControlled: true,
                builder: (_) => AddToListSheet(wordId: state.word!.wordId),
              ),
              icon: const Icon(Icons.playlist_add),
              label: const Text('Thêm vào danh sách'),
            ),
      body: _body(state),
    );
  }

  Widget _body(WordDetailState state) {
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    final word = state.word;
    if (word == null) {
      return Center(child: Text(state.errorMessage ?? 'Không tìm thấy từ.'));
    }
    if (!_hasAutoPlayed &&
        AppSettingsNotifier.instance.state.autoPlayPronunciation &&
        word.audio.isNotEmpty) {
      _hasAutoPlayed = true;
      final preferred = word.audio.where(
        (item) => item.accent.toUpperCase() == _accent,
      );
      final audio = preferred.isEmpty ? word.audio.first : preferred.first;
      WidgetsBinding.instance.addPostFrameCallback((_) => _play(audio.url));
    }
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 104),
      children: [
        if (state.isOffline) const _OfflineBanner(),
        _HeroSection(
          word: word,
          accent: _accent,
          onAccentChanged: (value) => setState(() => _accent = value),
          onPlay: _play,
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          key: const Key('save-word-book'),
          onPressed: state.isSaved
              ? null
              : ref
                    .read(wordDetailProvider(widget.wordId).notifier)
                    .saveToWordBook,
          icon: Icon(
            state.isSaved ? Icons.bookmark : Icons.bookmark_add_outlined,
          ),
          label: Text(
            state.isSaved ? 'Đã lưu vào sách từ' : 'Thêm vào sách từ',
          ),
        ),
        const SizedBox(height: 20),
        Text('Nghĩa của từ', style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 8),
        for (final sense in word.senses)
          _SenseTile(sense: sense, onRelatedTap: _openRelated),
        if (word.relations.isNotEmpty) ...[
          const SizedBox(height: 18),
          _RelatedWords(relations: word.relations, onTap: _openRelated),
        ],
        if (word.topics.isNotEmpty) ...[
          const SizedBox(height: 20),
          Text('Chủ đề', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: [
              for (final topic in word.topics)
                Chip(
                  key: Key('word-topic-${topic.topicId}'),
                  avatar: const Icon(Icons.tag, size: 16),
                  label: Text(topic.displayName),
                ),
            ],
          ),
        ],
      ],
    );
  }

  Future<void> _play(String url) async {
    try {
      await ref.read(audioPlaybackServiceProvider).play(url);
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Không thể phát âm thanh.')));
    }
  }

  void _openRelated(WordRelation relation) {
    if (relation.linkedWordId != null) {
      context.push(AppRoutes.wordDetail(relation.linkedWordId.toString()));
    }
  }
}

class _HeroSection extends StatelessWidget {
  const _HeroSection({
    required this.word,
    required this.accent,
    required this.onAccentChanged,
    required this.onPlay,
  });

  final WordDetail word;
  final String accent;
  final ValueChanged<String> onAccentChanged;
  final ValueChanged<String> onPlay;

  @override
  Widget build(BuildContext context) {
    final phonetic = accent == 'UK' ? word.phoneticUk : word.phoneticUs;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (word.imageUrl != null)
              ClipRRect(
                borderRadius: BorderRadius.circular(16),
                child: CachedNetworkImage(
                  imageUrl: word.imageUrl!,
                  height: 180,
                  width: double.infinity,
                  fit: BoxFit.cover,
                  errorWidget: (_, _, _) => const SizedBox.shrink(),
                ),
              ),
            if (word.imageUrl != null) const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: Text(
                    word.word,
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                if (word.cefr != null) Chip(label: Text(word.cefr!)),
              ],
            ),
            const SizedBox(height: 8),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'UK', label: Text('UK')),
                ButtonSegment(value: 'US', label: Text('US')),
              ],
              selected: {accent},
              onSelectionChanged: (values) => onAccentChanged(values.first),
            ),
            if (phonetic != null) ...[
              const SizedBox(height: 10),
              Text(
                phonetic,
                key: const Key('active-phonetic'),
                style: TextStyle(color: Theme.of(context).colorScheme.primary),
              ),
            ],
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              children: [
                for (final item in word.audio)
                  OutlinedButton.icon(
                    key: Key('play-${item.accent.toLowerCase()}'),
                    onPressed: () => onPlay(item.url),
                    icon: const Icon(Icons.volume_up),
                    label: Text('Phát ${item.accent.toUpperCase()}'),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _SenseTile extends StatelessWidget {
  const _SenseTile({required this.sense, required this.onRelatedTap});

  final WordSense sense;
  final ValueChanged<WordRelation> onRelatedTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ExpansionTile(
        key: Key('sense-${sense.senseId}'),
        initiallyExpanded: sense.order == 1,
        title: Row(
          children: [
            Chip(label: Text(sense.wordClass)),
            const SizedBox(width: 10),
            Expanded(child: Text(sense.englishDefinition)),
          ],
        ),
        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        expandedCrossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (sense.vietnameseMeaning != null)
            Text(
              sense.vietnameseMeaning!,
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
            ),
          for (final example in sense.examples)
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.format_quote),
              title: Text(example.exampleEn),
              subtitle: example.exampleVi == null
                  ? null
                  : Text(example.exampleVi!),
            ),
          if (sense.relations.isNotEmpty)
            _RelatedWords(relations: sense.relations, onTap: onRelatedTap),
        ],
      ),
    );
  }
}

class _RelatedWords extends StatelessWidget {
  const _RelatedWords({required this.relations, required this.onTap});

  final List<WordRelation> relations;
  final ValueChanged<WordRelation> onTap;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 4,
      children: [
        for (final relation in relations)
          ActionChip(
            key: Key('relation-${relation.relatedWord}'),
            backgroundColor: relation.relationType.toLowerCase() == 'antonym'
                ? AppColors.error.withValues(alpha: 0.18)
                : Colors.green.withValues(alpha: 0.18),
            label: Text(relation.relatedWord),
            onPressed: relation.linkedWordId == null
                ? null
                : () => onTap(relation),
          ),
      ],
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('word-detail-offline-banner'),
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(10),
      color: AppColors.error.withValues(alpha: 0.18),
      child: const Text('Đang hiển thị chi tiết từ đã lưu offline.'),
    );
  }
}
