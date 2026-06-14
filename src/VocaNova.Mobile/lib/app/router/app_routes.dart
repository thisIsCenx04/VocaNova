abstract final class AppRoutes {
  static const root = '/';
  static const login = '/login';
  static const register = '/register';
  static const forgotPassword = '/forgot-password';
  static const otp = '/otp';
  static const onboarding = '/onboarding';
  static const home = '/home';
  static const search = '/search';
  static const word = '/word/:id';
  static const lists = '/lists';
  static const list = '/list/:id';
  static const quizConfig = '/quiz/config';
  static const quizActive = '/quiz/active';
  static const quizResult = '/quiz/result';
  static const progress = '/progress';
  static const settings = '/settings';
  static const profile = '/profile';

  static String wordDetail(String id) => '/word/$id';

  static String listDetail(String id) => '/list/$id';
}
