abstract final class ApiEndpoints {
  static const health = '/health';

  static const register = '/api/auth/register';
  static const login = '/api/auth/login';
  static const googleLogin = '/api/auth/google';
  static const refreshToken = '/api/auth/refresh';
  static const logout = '/api/auth/logout';
  static const sendOtp = '/api/auth/otp/send';
  static const verifyOtp = '/api/auth/otp/verify';
  static const forgotPassword = '/api/auth/forgot-password';
  static const resetPassword = '/api/auth/reset-password';
  static const currentUser = '/api/auth/me';
  static const updateProfile = '/api/auth/me/profile';
  static const updateLearningProfile = '/api/auth/me/learning-profile';

  static const words = '/api/words';
  static const topics = '/api/topics';
  static const lists = '/api/lists';
}
