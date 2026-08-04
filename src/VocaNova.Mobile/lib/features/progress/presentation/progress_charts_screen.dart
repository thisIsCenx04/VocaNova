import 'dart:math' as math;

import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/progress/application/progress_charts_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_charts_state.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_analytics.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class ProgressChartsScreen extends ConsumerStatefulWidget {
  const ProgressChartsScreen({super.key});

  @override
  ConsumerState<ProgressChartsScreen> createState() =>
      _ProgressChartsScreenState();
}

class _ProgressChartsScreenState extends ConsumerState<ProgressChartsScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(progressChartsProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(progressChartsProvider);
    ref.listen(progressChartsProvider.select((value) => value.errorMessage), (
      _,
      next,
    ) {
      if (next != null) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next)));
      }
    });
    final l10n = AppLocalizations.of(context)!;
    return Scaffold(
      appBar: AppBar(title: Text(l10n.progressChartsTitle)),
      body: state.isLoading
          ? const Center(child: CircularProgressIndicator())
          : state.chart == null
          ? _ErrorState(onRetry: ref.read(progressChartsProvider.notifier).load)
          : RefreshIndicator(
              onRefresh: ref.read(progressChartsProvider.notifier).load,
              child: _ChartsContent(state: state),
            ),
    );
  }
}

class _ChartsContent extends ConsumerWidget {
  const _ChartsContent({required this.state});

  final ProgressChartsState state;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    return ListView(
      key: const Key('progress-charts-list'),
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                l10n.progressSessionsCountLabel,
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
            ),
            SizedBox(
              width: 190,
              child: DropdownButtonFormField<String>(
                key: const Key('chart-granularity'),
                initialValue: state.granularity,
                isExpanded: true,
                items: [
                  DropdownMenuItem(
                    value: 'daily',
                    child: Text(l10n.progressGranularityDaily),
                  ),
                  DropdownMenuItem(
                    value: 'weekly',
                    child: Text(l10n.progressGranularityWeekly),
                  ),
                  DropdownMenuItem(
                    value: 'monthly',
                    child: Text(l10n.progressGranularityMonthly),
                  ),
                ],
                onChanged: state.isLoadingChart
                    ? null
                    : (value) {
                        if (value != null) {
                          ref
                              .read(progressChartsProvider.notifier)
                              .changeGranularity(value);
                        }
                      },
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 20, 18, 12),
            child: SizedBox(
              key: const Key('sessions-line-chart'),
              height: 230,
              child: state.isLoadingChart
                  ? const Center(child: CircularProgressIndicator())
                  : _SessionsLineChart(chart: state.chart!),
            ),
          ),
        ),
        const SizedBox(height: 22),
        Text(
          l10n.progressMasteryLevelLabel,
          style: Theme.of(
            context,
          ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: SizedBox(
              key: const Key('mastery-bar-chart'),
              height: 280,
              child: _MasteryBarChart(items: state.mastery),
            ),
          ),
        ),
        const SizedBox(height: 22),
        Text(
          l10n.progressTop10WeakestWordsLabel,
          style: Theme.of(
            context,
          ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 12),
        if (state.weakestWords.isEmpty)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Text(l10n.progressNoWeakestWords),
            ),
          )
        else
          for (final word in state.weakestWords)
            _WeakestWordCard(
              word: word,
              onTap: () =>
                  context.push(AppRoutes.wordDetail(word.wordId.toString())),
            ),
      ],
    );
  }
}

class _SessionsLineChart extends StatelessWidget {
  const _SessionsLineChart({required this.chart});

  final ProgressChart chart;

  @override
  Widget build(BuildContext context) {
    final maxSessions = chart.points.fold<int>(
      1,
      (current, point) => math.max(current, point.sessionsCount),
    );
    return LineChart(
      LineChartData(
        minX: 0,
        maxX: math.max(1, chart.points.length - 1).toDouble(),
        minY: 0,
        maxY: (maxSessions + 1).toDouble(),
        gridData: const FlGridData(show: true, drawVerticalLine: false),
        titlesData: FlTitlesData(
          topTitles: const AxisTitles(
            sideTitles: SideTitles(showTitles: false),
          ),
          rightTitles: const AxisTitles(
            sideTitles: SideTitles(showTitles: false),
          ),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              interval: math.max(1, chart.points.length ~/ 5).toDouble(),
              getTitlesWidget: (value, meta) {
                final index = value.round();
                if (index < 0 || index >= chart.points.length) {
                  return const SizedBox.shrink();
                }
                return SideTitleWidget(
                  meta: meta,
                  child: Text(
                    chart.points[index].periodLabel,
                    style: const TextStyle(fontSize: 9),
                  ),
                );
              },
            ),
          ),
          leftTitles: const AxisTitles(
            sideTitles: SideTitles(showTitles: true, reservedSize: 28),
          ),
        ),
        lineBarsData: [
          LineChartBarData(
            spots: [
              for (var index = 0; index < chart.points.length; index++)
                FlSpot(
                  index.toDouble(),
                  chart.points[index].sessionsCount.toDouble(),
                ),
            ],
            isCurved: true,
            color: Theme.of(context).colorScheme.primary,
            barWidth: 3,
            dotData: const FlDotData(show: false),
            belowBarData: BarAreaData(
              show: true,
              color: Theme.of(
                context,
              ).colorScheme.primary.withValues(alpha: 0.16),
            ),
          ),
        ],
      ),
    );
  }
}

class _MasteryBarChart extends StatelessWidget {
  const _MasteryBarChart({required this.items});

  final List<MasteryBreakdown> items;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final maxCount = items.fold<int>(
      1,
      (current, item) => math.max(current, item.wordCount),
    );
    return Column(
      mainAxisAlignment: MainAxisAlignment.spaceEvenly,
      children: [
        for (final item in items)
          Row(
            key: Key('mastery-level-${item.masteryLevel}'),
            children: [
              SizedBox(
                width: 42,
                child: Text(l10n.progressMasteryLevelShort(item.masteryLevel)),
              ),
              Expanded(
                child: LinearProgressIndicator(
                  minHeight: 16,
                  borderRadius: BorderRadius.circular(8),
                  value: item.wordCount / maxCount,
                ),
              ),
              SizedBox(
                width: 40,
                child: Text('${item.wordCount}', textAlign: TextAlign.end),
              ),
            ],
          ),
      ],
    );
  }
}

class _WeakestWordCard extends StatelessWidget {
  const _WeakestWordCard({required this.word, required this.onTap});

  final WeakestWord word;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Card(
      key: Key('weakest-word-${word.wordId}'),
      margin: const EdgeInsets.only(bottom: 10),
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      word.word,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  Text('${word.accuracyRate.round()}%'),
                ],
              ),
              if (word.primaryMeaning != null) Text(word.primaryMeaning!),
              const SizedBox(height: 8),
              LinearProgressIndicator(
                value: word.accuracyRate.clamp(0, 100) / 100,
              ),
              const SizedBox(height: 6),
              Text(
                l10n.progressWordStatsLabel(word.correctCount, word.wrongCount),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: FilledButton(
        onPressed: onRetry,
        child: Text(AppLocalizations.of(context)!.progressRetry),
      ),
    );
  }
}
