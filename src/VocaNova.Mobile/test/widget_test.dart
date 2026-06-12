import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/app/app.dart';

void main() {
  testWidgets('VocaNova app renders the themed home screen', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: VocaNovaApp()));
    await tester.pumpAndSettle();

    expect(find.text('VocaNova'), findsOneWidget);
    expect(find.text('Học từ vựng mỗi ngày'), findsOneWidget);
    expect(find.text('Khám phá VocaNova'), findsOneWidget);
  });
}
