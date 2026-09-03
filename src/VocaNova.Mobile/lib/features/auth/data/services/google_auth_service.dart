import 'package:google_sign_in/google_sign_in.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

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
    final l10n = _l10n;
    if (serverClientId.isEmpty) {
      throw FormatException(l10n.authGoogleClientIdMissing);
    }

    try {
      await (_initialization ??= _googleSignIn.initialize(
        serverClientId: serverClientId,
      ));
      final account = await _googleSignIn.authenticate();
      final idToken = account.authentication.idToken;
      if (idToken == null || idToken.isEmpty) {
        throw FormatException(l10n.authGoogleTokenMissing);
      }
      return idToken;
    } on GoogleSignInException catch (error) {
      if (isCanceled(error)) {
        throw FormatException(l10n.authGoogleCanceled);
      }
      throw FormatException(_googleErrorMessage(error, l10n));
    }
  }

  Future<void> signOut() async {
    await (_initialization ??= _googleSignIn.initialize(
      serverClientId: serverClientId.isEmpty ? null : serverClientId,
    ));
    await _googleSignIn.signOut();
  }

  static bool isCanceled(Object error) {
    return error is GoogleSignInException &&
        error.code == GoogleSignInExceptionCode.canceled;
  }

  AppLocalizations get _l10n =>
      lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);

  static String _googleErrorMessage(
    GoogleSignInException error,
    AppLocalizations l10n,
  ) {
    return switch (error.code) {
      GoogleSignInExceptionCode.clientConfigurationError =>
        l10n.authGoogleClientConfigurationError,
      GoogleSignInExceptionCode.providerConfigurationError =>
        l10n.authGoogleProviderConfigurationError,
      GoogleSignInExceptionCode.uiUnavailable => l10n.authGoogleUiUnavailable,
      GoogleSignInExceptionCode.interrupted => l10n.authGoogleInterrupted,
      _ => l10n.authGoogleUnknownError(error.description ?? error.code.name),
    };
  }
}
