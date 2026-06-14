import 'package:google_sign_in/google_sign_in.dart';

class GoogleAuthService {
  GoogleAuthService({
    GoogleSignIn? googleSignIn,
    this.serverClientId = const String.fromEnvironment(
      'GOOGLE_SERVER_CLIENT_ID',
    ),
  }) : _googleSignIn = googleSignIn ?? GoogleSignIn.instance;

  final GoogleSignIn _googleSignIn;
  final String serverClientId;
  Future<void>? _initialization;

  Future<String> getIdToken() async {
    await (_initialization ??= _googleSignIn.initialize(
      serverClientId: serverClientId.isEmpty ? null : serverClientId,
    ));
    final account = await _googleSignIn.authenticate();
    final idToken = account.authentication.idToken;
    if (idToken == null || idToken.isEmpty) {
      throw const FormatException('Google không trả về mã đăng nhập hợp lệ.');
    }
    return idToken;
  }
}
