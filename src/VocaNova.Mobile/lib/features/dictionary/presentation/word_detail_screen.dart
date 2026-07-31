import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_state.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/add_to_list_sheet.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class WordDetailScreen extends ConsumerStatefulWidget {
  const WordDetailScreen({required this.wordId, super.key});

  final int wordId;

  @override
  ConsumerState<WordDetailScreen> createState() => _WordDetailScreenState();
}

class _WordDetailScreenState extends ConsumerState<WordDetailScreen> {
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
    final palette = _DetailPalette.of(context);
    return Scaffold(
      key: const Key('word-detail-screen'),
      backgroundColor: palette.background,
      body: SafeArea(
        bottom: false,
        child: switch (state.word) {
          null when state.isLoading => const Center(
            child: CircularProgressIndicator(),
          ),
          null => _LoadError(
            message:
                state.errorMessage ??
                AppLocalizations.of(context)!.dictWordNotFound,
            onBack: _goBack,
          ),
          final word => _detail(state, word),
        },
      ),
    );
  }

  Widget _detail(WordDetailState state, WordDetail word) {
    _autoPlay(word);
    final relations = _allRelations(word);
    return Column(
      children: [
        _DetailHeader(
          word: word,
          isSaved: state.isSaved,
          onBack: _goBack,
          onShare: () => _share(word),
          onSave: state.isSaved
              ? null
              : ref
                    .read(wordDetailProvider(widget.wordId).notifier)
                    .saveToWordBook,
          onPlay: _playPronunciation,
        ),
        if (state.isOffline) const _OfflineBanner(),
        Expanded(
          child: ListView(
            key: const Key('word-detail-content'),
            padding: EdgeInsets.zero,
            children: [
              if (word.imageUrl != null) _WordImage(url: word.imageUrl!),
              for (final sense in word.senses) ...[
                _TextSection(
                  label: AppLocalizations.of(context)!.dictDefinitionLabel,
                  child: Text(
                    sense.englishDefinition,
                    style: _bodyStyle(context),
                  ),
                ),
                if (sense.vietnameseMeaning?.trim().isNotEmpty == true)
                  _TextSection(
                    label: AppLocalizations.of(
                      context,
                    )!.dictVietnameseMeaningLabel,
                    child: Text(
                      sense.vietnameseMeaning!,
                      style: _bodyStyle(
                        context,
                      ).copyWith(fontWeight: FontWeight.w500),
                    ),
                  ),
                if (sense.examples.isNotEmpty)
                  _ExamplesSection(examples: sense.examples),
              ],
              if (word.examples.isNotEmpty)
                _ExamplesSection(examples: word.examples),
              if (relations.isNotEmpty)
                _RelationsSection(relations: relations, onTap: _openRelated),
              if (word.topics.isNotEmpty) _TopicsSection(topics: word.topics),
            ],
          ),
        ),
        _DetailActions(
          onAddToList: () => _showAddToList(word),
          onPractice: () => context.push(AppRoutes.quizConfig),
        ),
      ],
    );
  }

  TextStyle _bodyStyle(BuildContext context) {
    return AppTextStyles.body.copyWith(
      color: _DetailPalette.of(context).text,
      fontSize: 16,
      height: 26.4 / 16,
    );
  }

  void _autoPlay(WordDetail word) {
    if (_hasAutoPlayed ||
        !AppSettingsNotifier.instance.state.autoPlayPronunciation ||
        word.word.trim().isEmpty) {
      return;
    }
    _hasAutoPlayed = true;
    final uk = word.audio.where((item) => item.accent.toUpperCase() == 'UK');
    final audio = uk.isNotEmpty
        ? uk.first
        : (word.audio.isEmpty ? null : word.audio.first);
    WidgetsBinding.instance.addPostFrameCallback(
      (_) => _playPronunciation(audio?.url, word.word, audio?.accent ?? 'UK'),
    );
  }

  List<WordRelation> _allRelations(WordDetail word) {
    final unique = <String, WordRelation>{};
    for (final relation in [
      ...word.relations,
      ...word.senses.expand((sense) => sense.relations),
    ]) {
      unique['${relation.relationType}:${relation.relatedWord}'] = relation;
    }
    return unique.values.toList(growable: false);
  }

  Future<void> _playPronunciation(
    String? audioUrl,
    String word,
    String accent,
  ) async {
    try {
      await ref
          .read(audioPlaybackServiceProvider)
          .playPronunciation(word: word, accent: accent, audioUrl: audioUrl);
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(AppLocalizations.of(context)!.dictUnableToPlayAudio),
        ),
      );
    }
  }

  Future<void> _share(WordDetail word) async {
    final definition = word.senses.isEmpty
        ? ''
        : '\n${word.senses.first.englishDefinition}';
    await Clipboard.setData(ClipboardData(text: '${word.word}$definition'));
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(AppLocalizations.of(context)!.dictWordCopied),
      ),
    );
  }

  void _goBack() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRoutes.search);
    }
  }

  void _openRelated(WordRelation relation) {
    if (relation.linkedWordId != null) {
      context.push(AppRoutes.wordDetail(relation.linkedWordId.toString()));
    }
  }

  Future<void> _showAddToList(WordDetail word) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: false,
      backgroundColor: Colors.transparent,
      barrierColor: Colors.black.withValues(alpha: 0.38),
      builder: (_) =>
          AddToListSheet(wordId: word.wordId, eligibleTopics: word.topics),
    );
  }
}

class _DetailHeader extends StatelessWidget {
  const _DetailHeader({
    required this.word,
    required this.isSaved,
    required this.onBack,
    required this.onShare,
    required this.onSave,
    required this.onPlay,
  });

  final WordDetail word;
  final bool isSaved;
  final VoidCallback onBack;
  final VoidCallback onShare;
  final VoidCallback? onSave;
  final void Function(String? audioUrl, String word, String accent) onPlay;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    final wordClass = word.senses.isEmpty ? null : word.senses.first.wordClass;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 21),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border(bottom: BorderSide(color: palette.border, width: 1.25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            height: 32,
            child: Row(
              children: [
                _PlainIconButton(
                  key: const Key('word-detail-back'),
                  tooltip: AppLocalizations.of(context)!.dictBackTooltip,
                  icon: Icons.arrow_back,
                  onPressed: onBack,
                ),
                const Spacer(),
                _RoundIconButton(
                  key: const Key('share-word'),
                  tooltip: AppLocalizations.of(context)!.dictShareTooltip,
                  icon: Icons.share_outlined,
                  onPressed: onShare,
                ),
                const SizedBox(width: 8),
                _RoundIconButton(
                  key: const Key('save-word-book'),
                  tooltip: isSaved
                      ? AppLocalizations.of(context)!.dictSavedTooltip
                      : AppLocalizations.of(context)!.dictSaveWordTooltip,
                  icon: isSaved ? Icons.bookmark : Icons.bookmark_border,
                  onPressed: onSave,
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          Text(
            word.word,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: AppTextStyles.heading.copyWith(
              color: palette.text,
              fontSize: 34,
              height: 34 / 34,
              letterSpacing: -0.5,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              if (wordClass != null)
                _Badge(
                  label: wordClass,
                  background: const Color(0xFFF3E8FF),
                  foreground: const Color(0xFF6B21A8),
                ),
              if (wordClass != null && word.cefr != null)
                const SizedBox(width: 8),
              if (word.cefr != null)
                _Badge(
                  label: word.cefr!,
                  background: const Color(0xFFFEE2E2),
                  foreground: const Color(0xFF991B1B),
                ),
            ],
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              if (word.phoneticUk != null)
                _PronunciationButton(
                  key: const Key('play-uk'),
                  accent: 'UK',
                  phonetic: word.phoneticUk!,
                  audioUrl: _audioUrl(word.audio, 'UK'),
                  word: word.word,
                  onPlay: onPlay,
                ),
              if (word.phoneticUs != null)
                _PronunciationButton(
                  key: const Key('play-us'),
                  accent: 'US',
                  phonetic: word.phoneticUs!,
                  audioUrl: _audioUrl(word.audio, 'US'),
                  word: word.word,
                  onPlay: onPlay,
                ),
            ],
          ),
        ],
      ),
    );
  }

  String? _audioUrl(List<WordAudio> audio, String accent) {
    final matches = audio.where((item) => item.accent.toUpperCase() == accent);
    return matches.isEmpty ? null : matches.first.url;
  }
}

class _PlainIconButton extends StatelessWidget {
  const _PlainIconButton({
    required this.icon,
    required this.onPressed,
    required this.tooltip,
    super.key,
  });

  final IconData icon;
  final VoidCallback onPressed;
  final String tooltip;

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: tooltip,
      onPressed: onPressed,
      padding: EdgeInsets.zero,
      constraints: const BoxConstraints.tightFor(width: 32, height: 32),
      icon: Icon(icon, size: 20, color: _DetailPalette.of(context).text),
    );
  }
}

class _RoundIconButton extends StatelessWidget {
  const _RoundIconButton({
    required this.icon,
    required this.onPressed,
    required this.tooltip,
    super.key,
  });

  final IconData icon;
  final VoidCallback? onPressed;
  final String tooltip;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Material(
      color: palette.input,
      shape: const CircleBorder(),
      child: IconButton(
        tooltip: tooltip,
        onPressed: onPressed,
        padding: EdgeInsets.zero,
        constraints: const BoxConstraints.tightFor(width: 32, height: 32),
        icon: Icon(icon, size: 15, color: palette.secondaryText),
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({
    required this.label,
    required this.background,
    required this.foreground,
  });

  final String label;
  final Color background;
  final Color foreground;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 20,
      padding: const EdgeInsets.symmetric(horizontal: 8),
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        label,
        style: AppTextStyles.caption.copyWith(
          color: foreground,
          fontSize: 12,
          height: 16 / 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _PronunciationButton extends StatelessWidget {
  const _PronunciationButton({
    required this.accent,
    required this.phonetic,
    required this.audioUrl,
    required this.word,
    required this.onPlay,
    super.key,
  });

  final String accent;
  final String phonetic;
  final String? audioUrl;
  final String word;
  final void Function(String? audioUrl, String word, String accent) onPlay;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Material(
      color: palette.input,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: palette.border, width: 1.25),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => onPlay(audioUrl, word, accent),
        child: Container(
          height: 38,
          padding: const EdgeInsets.symmetric(horizontal: 13),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                accent,
                style: AppTextStyles.caption.copyWith(
                  color: palette.secondaryText,
                  fontSize: 11,
                  height: 16.5 / 11,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                phonetic,
                key: accent == 'UK' ? const Key('active-phonetic') : null,
                style: AppTextStyles.caption.copyWith(
                  color: palette.text,
                  fontSize: 13,
                  height: 19.5 / 13,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(width: 8),
              Icon(
                Icons.volume_up_outlined,
                size: 13,
                color: audioUrl == null
                    ? palette.secondaryText
                    : AppColors.primary,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TextSection extends StatelessWidget {
  const _TextSection({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 21),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border(bottom: BorderSide(color: palette.border, width: 1.25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [_SectionLabel(label), const SizedBox(height: 8), child],
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.label, {this.muted = false});

  final String label;
  final bool muted;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Text(
      label.toUpperCase(),
      style: AppTextStyles.caption.copyWith(
        color: muted
            ? palette.secondaryText.withValues(alpha: 0.75)
            : palette.secondaryText,
        fontSize: 11,
        height: 16.5 / 11,
        letterSpacing: 0.5,
        fontWeight: FontWeight.w600,
      ),
    );
  }
}

class _ExamplesSection extends StatelessWidget {
  const _ExamplesSection({required this.examples});

  final List<WordExample> examples;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 21),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border(bottom: BorderSide(color: palette.border, width: 1.25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionLabel(AppLocalizations.of(context)!.dictExampleLabel),
          const SizedBox(height: 10),
          for (var index = 0; index < examples.length; index++) ...[
            _ExampleBlock(example: examples[index]),
            if (index != examples.length - 1) const SizedBox(height: 14),
          ],
        ],
      ),
    );
  }
}

class _ExampleBlock extends StatelessWidget {
  const _ExampleBlock({required this.example});

  final WordExample example;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Container(
      padding: const EdgeInsets.only(left: 13),
      decoration: const BoxDecoration(
        border: Border(left: BorderSide(color: AppColors.primary, width: 1.25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '“${example.exampleEn}”',
            style: AppTextStyles.body.copyWith(
              color: palette.text,
              fontSize: 15,
              height: 24 / 15,
              fontWeight: FontWeight.w700,
              fontStyle: FontStyle.italic,
            ),
          ),
          if (example.exampleVi?.trim().isNotEmpty == true) ...[
            const SizedBox(height: 6),
            Text(
              example.exampleVi!,
              style: AppTextStyles.caption.copyWith(
                color: palette.secondaryText,
                fontSize: 13,
                height: 19.5 / 13,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _RelationsSection extends StatelessWidget {
  const _RelationsSection({required this.relations, required this.onTap});

  final List<WordRelation> relations;
  final ValueChanged<WordRelation> onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    final synonyms = relations
        .where((item) => item.relationType.toLowerCase() == 'synonym')
        .toList();
    final antonyms = relations
        .where((item) => item.relationType.toLowerCase() == 'antonym')
        .toList();
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      color: palette.surface,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (synonyms.isNotEmpty) ...[
            _SectionLabel(AppLocalizations.of(context)!.dictSynonymsLabel),
            const SizedBox(height: 8),
            _RelationChips(
              relations: synonyms,
              color: AppColors.primary,
              onTap: onTap,
            ),
          ],
          if (synonyms.isNotEmpty && antonyms.isNotEmpty)
            const SizedBox(height: 16),
          if (antonyms.isNotEmpty) ...[
            _SectionLabel(
              AppLocalizations.of(context)!.dictAntonymsLabel,
              muted: true,
            ),
            const SizedBox(height: 8),
            _RelationChips(
              relations: antonyms,
              color: const Color(0xFFDC2626),
              onTap: onTap,
            ),
          ],
        ],
      ),
    );
  }
}

class _RelationChips extends StatelessWidget {
  const _RelationChips({
    required this.relations,
    required this.color,
    required this.onTap,
  });

  final List<WordRelation> relations;
  final Color color;
  final ValueChanged<WordRelation> onTap;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final relation in relations)
          Material(
            color: palette.input,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
              side: BorderSide(color: palette.border, width: 1.25),
            ),
            clipBehavior: Clip.antiAlias,
            child: InkWell(
              key: Key('relation-${relation.relatedWord}'),
              onTap: relation.linkedWordId == null
                  ? null
                  : () => onTap(relation),
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 7,
                ),
                child: Text(
                  relation.relatedWord,
                  style: AppTextStyles.caption.copyWith(
                    color: color,
                    fontSize: 13,
                    height: 19.5 / 13,
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class _TopicsSection extends StatelessWidget {
  const _TopicsSection({required this.topics});

  final List<WordTopic> topics;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
      color: palette.surface,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionLabel(AppLocalizations.of(context)!.dictTopicsTitle),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final topic in topics)
                Container(
                  key: Key('word-topic-${topic.topicId}'),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 7,
                  ),
                  decoration: BoxDecoration(
                    color: palette.input,
                    border: Border.all(color: palette.border, width: 1.25),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    topic.name,
                    style: AppTextStyles.caption.copyWith(
                      color: AppColors.primary,
                      fontSize: 13,
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _WordImage extends StatelessWidget {
  const _WordImage({required this.url});

  final String url;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(20),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(14),
        child: CachedNetworkImage(
          imageUrl: url,
          height: 180,
          width: double.infinity,
          fit: BoxFit.cover,
          errorWidget: (_, _, _) => const SizedBox.shrink(),
        ),
      ),
    );
  }
}

class _DetailActions extends StatelessWidget {
  const _DetailActions({required this.onAddToList, required this.onPractice});

  final VoidCallback onAddToList;
  final VoidCallback onPractice;

  @override
  Widget build(BuildContext context) {
    final palette = _DetailPalette.of(context);
    return Container(
      height: 81,
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
      decoration: BoxDecoration(
        color: palette.surface,
        border: Border(top: BorderSide(color: palette.border, width: 1.25)),
      ),
      child: Row(
        children: [
          Expanded(
            child: SizedBox(
              height: 48,
              child: OutlinedButton.icon(
                key: const Key('add-to-list-fab'),
                onPressed: onAddToList,
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.primary,
                  side: const BorderSide(color: AppColors.primary, width: 1.25),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
                icon: const Icon(Icons.add, size: 16),
                label: Text(AppLocalizations.of(context)!.dictAddToListTooltip),
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: SizedBox(
              height: 48,
              child: FilledButton.icon(
                key: const Key('practice-word'),
                onPressed: onPractice,
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
                icon: const Icon(Icons.play_arrow_outlined, size: 16),
                label: Text(AppLocalizations.of(context)!.dictPracticeLabel),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('word-detail-offline-banner'),
      width: double.infinity,
      color: const Color(0xFFFFF1C2),
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 7),
      child: Text(
        AppLocalizations.of(context)!.dictWordDetailOfflineBanner,
        textAlign: TextAlign.center,
      ),
    );
  }
}

class _LoadError extends StatelessWidget {
  const _LoadError({required this.message, required this.onBack});

  final String message;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerLeft,
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: _PlainIconButton(
              icon: Icons.arrow_back,
              onPressed: onBack,
              tooltip: AppLocalizations.of(context)!.dictBackTooltip,
            ),
          ),
        ),
        Expanded(child: Center(child: Text(message))),
      ],
    );
  }
}

class _DetailPalette {
  const _DetailPalette({
    required this.background,
    required this.surface,
    required this.input,
    required this.text,
    required this.secondaryText,
    required this.border,
  });

  final Color background;
  final Color surface;
  final Color input;
  final Color text;
  final Color secondaryText;
  final Color border;

  factory _DetailPalette.of(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    return _DetailPalette(
      background: dark ? AppColors.background : const Color(0xFFF7F7F8),
      surface: dark ? AppColors.surface : Colors.white,
      input: dark ? const Color(0xFF292929) : const Color(0xFFF7F7F8),
      text: dark ? AppColors.onSurface : const Color(0xFF111111),
      secondaryText: dark ? const Color(0xFFAAA6B3) : const Color(0xFF444444),
      border: dark ? const Color(0xFF3A3A3A) : const Color(0xFFEFEFEF),
    );
  }
}
