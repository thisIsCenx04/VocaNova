import 'package:dio/dio.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

String authRequestError(Object error) {
  if (error is AppException) {
    return error.message;
  }
  if (error is DioException && error.error is AppException) {
    return (error.error! as AppException).message;
  }
  if (error is FormatException) {
    return error.message;
  }
  final l10n = lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);
  return l10n.authGenericError;
}
