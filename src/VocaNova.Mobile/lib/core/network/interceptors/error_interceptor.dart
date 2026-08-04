import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';

class ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.error is AppException) {
      handler.next(err);
      return;
    }

    final statusCode = err.response?.statusCode;
    final errors = _readErrors(err.response?.data);
    final message = _messageFor(err, errors);
    final exception = AppException(
      message,
      statusCode: statusCode,
      errors: errors,
    );

    handler.next(err.copyWith(error: exception, message: message));
  }

  List<String> _readErrors(dynamic data) {
    if (data is! Map) {
      return const [];
    }

    final rawErrors = data['errors'];
    if (rawErrors is! List) {
      return const [];
    }

    return rawErrors
        .whereType<Object>()
        .map((error) => error.toString())
        .where((error) => error.trim().isNotEmpty)
        .toList(growable: false);
  }

  String _messageFor(DioException error, List<String> errors) {
    final translatedError = errors
        .map(_translateBackendMessage)
        .whereType<String>()
        .firstOrNull;
    if (translatedError != null) {
      return translatedError;
    }

    return switch (error.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout =>
        'Kết nối quá thời gian. Vui lòng thử lại.',
      DioExceptionType.connectionError =>
        'Không thể kết nối đến máy chủ. Vui lòng kiểm tra mạng.',
      _ => _statusMessage(error.response?.statusCode),
    };
  }

  String? _translateBackendMessage(String message) {
    final normalized = message.trim().toLowerCase();
    if (normalized.contains('invalid phone or password')) {
      return 'Số điện thoại hoặc mật khẩu không đúng.';
    }
    if (normalized.contains('maximum otp verify attempts exceeded')) {
      return 'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.';
    }
    if (normalized.contains('otp has expired')) {
      return 'Mã OTP đã hết hạn. Vui lòng gửi lại mã.';
    }
    if (normalized.contains('otp has already been used')) {
      return 'Mã OTP đã được sử dụng. Vui lòng gửi lại mã.';
    }
    if (normalized.contains('invalid phone or otp') ||
        normalized.contains('invalid otp')) {
      return 'Mã OTP không đúng.';
    }
    if (normalized.contains('invalid refresh token') ||
        normalized.contains('refresh token has expired') ||
        normalized.contains('refresh token has been revoked')) {
      return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
    }
    if (normalized.contains('unauthorized')) {
      return 'Bạn cần đăng nhập để tiếp tục.';
    }
    if (normalized.contains('locked')) {
      return 'Tài khoản đã bị khóa.';
    }
    if (normalized.contains('not found')) {
      return 'Không tìm thấy dữ liệu yêu cầu.';
    }
    if (normalized.contains('phone already exists')) {
      return 'Tài khoản đã tồn tại. Vui lòng thay đổi số điện thoại.';
    }
    if (normalized.contains(
      'new password must be different from current password',
    )) {
      return 'Mật khẩu mới trùng với mật khẩu cũ. Vui lòng chọn mật khẩu khác hoặc đăng nhập lại bằng mật khẩu cũ.';
    }
    if (normalized.contains('already exists') ||
        normalized.contains('conflict')) {
      return 'Dữ liệu đã tồn tại.';
    }
    if (normalized.contains('rate limit') ||
        normalized.contains('too many requests')) {
      return 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.';
    }
    return null;
  }

  String _statusMessage(int? statusCode) {
    return switch (statusCode) {
      400 => 'Dữ liệu gửi lên không hợp lệ.',
      401 => 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
      403 => 'Bạn không có quyền thực hiện thao tác này.',
      404 => 'Không tìm thấy dữ liệu yêu cầu.',
      409 => 'Dữ liệu đã tồn tại.',
      422 => 'Dữ liệu gửi lên không hợp lệ.',
      429 => 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.',
      500 => 'Máy chủ đang gặp sự cố. Vui lòng thử lại sau.',
      _ => 'Đã xảy ra lỗi. Vui lòng thử lại.',
    };
  }
}
