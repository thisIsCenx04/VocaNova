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
  static const quizSessions = '/api/quiz/sessions';
  static const quizWrongWords = '/api/quiz/wrong-words';
  static const progressSummary = '/api/progress/summary';

  static String word(int id) => '$words/$id';

  static String listWords(int id) => '$lists/$id/words';

  static String list(int id) => '$lists/$id';

  static String listWord(int listId, int wordId) =>
      '${listWords(listId)}/$wordId';

  static String listRandomWords(int id) => '${listWords(id)}/random';

  static String quizAnswer(int id) => '$quizSessions/$id/answer';

  static String quizFinish(int id) => '$quizSessions/$id/finish';

  static String quizResult(int id) => '$quizSessions/$id/result';

  static String quizWrongWord(int id) => '$quizWrongWords/$id';
}
