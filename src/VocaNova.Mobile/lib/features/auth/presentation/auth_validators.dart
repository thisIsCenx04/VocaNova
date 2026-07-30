import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

AppLocalizations get _l10n =>
    lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);

abstract final class AuthValidators {
  static final _phonePattern = RegExp(r'^0[3-9]\d{8}$');
  static final _uppercasePattern = RegExp('[A-Z]');
  static final _lowercasePattern = RegExp('[a-z]');
  static final _digitPattern = RegExp(r'\d');

  static String? phone(String? value) {
    final phone = value?.trim() ?? '';
    if (phone.isEmpty) {
      return _l10n.authPhoneRequired;
    }
    if (!_phonePattern.hasMatch(phone)) {
      return _l10n.authPhoneInvalid;
    }
    return null;
  }

  static String? loginPassword(String? value) {
    if (value == null || value.isEmpty) {
      return _l10n.authPasswordRequired;
    }
    return null;
  }

  static String? strongPassword(String? value) {
    final password = value ?? '';
    if (password.isEmpty) {
      return _l10n.authPasswordRequired;
    }
    if (password.length < 8) {
      return _l10n.authPasswordTooShort;
    }
    if (!_uppercasePattern.hasMatch(password) ||
        !_lowercasePattern.hasMatch(password) ||
        !_digitPattern.hasMatch(password)) {
      return _l10n.authPasswordComplexity;
    }
    return null;
  }

  static String? displayName(String? value) {
    final name = value?.trim() ?? '';
    if (name.isEmpty) {
      return _l10n.authDisplayNameRequired;
    }
    if (name.length < 2) {
      return _l10n.authDisplayNameTooShort;
    }
    if (name.length > 150) {
      return _l10n.authDisplayNameTooLong;
    }
    return null;
  }

  static String? confirmPassword(String? value, String password) {
    if (value == null || value.isEmpty) {
      return _l10n.authConfirmPasswordRequired;
    }
    if (value != password) {
      return _l10n.authConfirmPasswordMismatch;
    }
    return null;
  }
}
