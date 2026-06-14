import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';

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
  return 'Đã xảy ra lỗi. Vui lòng thử lại.';
}
