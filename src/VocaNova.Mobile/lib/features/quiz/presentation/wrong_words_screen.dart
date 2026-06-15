import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_state.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';

class WrongWordsScreen extends ConsumerStatefulWidget {
  const WrongWordsScreen({super.key});

  @override
  ConsumerState<WrongWordsScreen> createState() => _WrongWordsScreenState();
}

class _WrongWordsScreenState extends ConsumerState<WrongWordsScreen> {
  late final ScrollController _controller;

  @override
  void initState() {
    super.initState();
    _controller = ScrollController()..addListener(_onScroll);
    Future.microtask(() => ref.read(wrongWordsProvider.notifier).load());
  }

  @override
  void dispose() {
    _controller
      ..removeListener(_onScroll)
      ..dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(wrongWordsProvider);
    ref.listen(wrongWordsProvider.select((value) => value.errorMessage), (
      _,
      next,
    ) {
      if (next != null) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next)));
      }
    });
    return Scaffold(
      appBar: AppBar(title: const Text('Từ trả lời sai')),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.all(16),
        child: FilledButton.icon(
          key: const Key('retry-wrong-words-button'),
          onPressed: state.words.isEmpty
              ? null
              : () => context.go(AppRoutes.quizConfig),
          icon: const Icon(Icons.quiz_outlined),
          label: const Text('Test lại'),
        ),
      ),
      body: _content(state),
    );
  }

  Widget _content(WrongWordsState state) {
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.words.isEmpty) {
      return const Center(
        child: Text('Bạn chưa có từ nào trong danh sách sai.'),
      );
    }
    return RefreshIndicator(
      onRefresh: ref.read(wrongWordsProvider.notifier).load,
      child: ListView.builder(
        key: const Key('wrong-words-list'),
        controller: _controller,
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
        itemCount: state.words.length + (state.isLoadingMore ? 1 : 0),
        itemBuilder: (_, index) {
          if (index == state.words.length) {
            return const Center(child: CircularProgressIndicator());
          }
          final word = state.words[index];
          return Dismissible(
            key: Key('wrong-word-${word.wordId}'),
            direction: DismissDirection.endToStart,
            onDismissed: (_) =>
                ref.read(wrongWordsProvider.notifier).remove(word.wordId),
            background: Container(
              alignment: Alignment.centerRight,
              color: Colors.red,
              padding: const EdgeInsets.only(right: 24),
              child: const Icon(Icons.delete, color: Colors.white),
            ),
            child: _WrongWordTile(
              word: word,
              onTap: () =>
                  context.push(AppRoutes.wordDetail(word.wordId.toString())),
            ),
          );
        },
      ),
    );
  }

  void _onScroll() {
    if (_controller.position.extentAfter < 300) {
      ref.read(wrongWordsProvider.notifier).loadMore();
    }
  }
}

class _WrongWordTile extends StatelessWidget {
  const _WrongWordTile({required this.word, required this.onTap});

  final WrongWord word;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        onTap: onTap,
        title: Text(word.word),
        subtitle: Text(
          '${word.primaryMeaning ?? "Chưa có nghĩa"}\n'
          'Đúng: ${word.correctCount} · Sai: ${word.wrongCount}',
        ),
        trailing: Text('Lv.${word.masteryLevel}'),
      ),
    );
  }
}
