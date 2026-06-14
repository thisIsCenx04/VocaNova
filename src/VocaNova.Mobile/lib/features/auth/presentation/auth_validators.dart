abstract final class AuthValidators {
  static final _phonePattern = RegExp(r'^0[3-9]\d{8}$');
  static final _uppercasePattern = RegExp('[A-Z]');
  static final _lowercasePattern = RegExp('[a-z]');
  static final _digitPattern = RegExp(r'\d');

  static String? phone(String? value) {
    final phone = value?.trim() ?? '';
    if (phone.isEmpty) {
      return 'Vui lòng nhập số điện thoại.';
    }
    if (!_phonePattern.hasMatch(phone)) {
      return 'Số điện thoại Việt Nam không hợp lệ.';
    }
    return null;
  }

  static String? loginPassword(String? value) {
    if (value == null || value.isEmpty) {
      return 'Vui lòng nhập mật khẩu.';
    }
    return null;
  }

  static String? strongPassword(String? value) {
    final password = value ?? '';
    if (password.isEmpty) {
      return 'Vui lòng nhập mật khẩu.';
    }
    if (password.length < 8) {
      return 'Mật khẩu phải có ít nhất 8 ký tự.';
    }
    if (!_uppercasePattern.hasMatch(password) ||
        !_lowercasePattern.hasMatch(password) ||
        !_digitPattern.hasMatch(password)) {
      return 'Mật khẩu cần có chữ hoa, chữ thường và chữ số.';
    }
    return null;
  }

  static String? displayName(String? value) {
    final name = value?.trim() ?? '';
    if (name.isEmpty) {
      return 'Vui lòng nhập tên hiển thị.';
    }
    if (name.length < 2) {
      return 'Tên hiển thị phải có ít nhất 2 ký tự.';
    }
    if (name.length > 150) {
      return 'Tên hiển thị không được vượt quá 150 ký tự.';
    }
    return null;
  }

  static String? confirmPassword(String? value, String password) {
    if (value == null || value.isEmpty) {
      return 'Vui lòng xác nhận mật khẩu.';
    }
    if (value != password) {
      return 'Mật khẩu xác nhận không khớp.';
    }
    return null;
  }
}
