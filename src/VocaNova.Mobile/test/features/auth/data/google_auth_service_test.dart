import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/features/auth/data/services/google_auth_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('explains how to configure a missing Google server client ID', () async {
    final service = GoogleAuthService(serverClientId: '');

    await expectLater(
      service.getIdToken(),
      throwsA(
        isA<FormatException>().having(
          (error) => error.message,
          'message',
          contains('GOOGLE_SERVER_CLIENT_ID'),
        ),
      ),
    );
  });
}
