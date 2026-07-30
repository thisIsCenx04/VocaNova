import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/widgets/offline_banner.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  testWidgets('stream provider shows shared offline banner', (tester) async {
    final service = MockConnectivityService();
    when(() => service.isOnline).thenAnswer((_) async => false);
    when(() => service.statusStream).thenAnswer((_) => Stream.value(false));

    await tester.pumpWidget(
      ProviderScope(
        overrides: [connectivityServiceProvider.overrideWithValue(service)],
        child: MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: const ConnectivityProbe(),
        ),
      ),
    );
    await tester.pump();

    expect(find.byKey(const Key('global-offline-banner')), findsOneWidget);
    expect(find.text("You're offline"), findsOneWidget);
  });
}

class ConnectivityProbe extends ConsumerWidget {
  const ConnectivityProbe({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isOnline = ref.watch(connectivityProvider).value ?? true;
    return Scaffold(body: isOnline ? const SizedBox() : const OfflineBanner());
  }
}

class MockConnectivityService extends Mock implements ConnectivityService {}
