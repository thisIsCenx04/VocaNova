class AppException implements Exception {
  const AppException(this.message, {this.statusCode, this.errors = const []});

  final String message;
  final int? statusCode;
  final List<String> errors;

  @override
  String toString() => message;
}
