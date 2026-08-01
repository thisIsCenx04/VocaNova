// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Vietnamese (`vi`).
class AppLocalizationsVi extends AppLocalizations {
  AppLocalizationsVi([String locale = 'vi']) : super(locale);

  @override
  String get appTitle => 'VocaNova';

  @override
  String get navHome => 'Trang chủ';

  @override
  String get navSearch => 'Tìm kiếm';

  @override
  String get navLists => 'Danh sách';

  @override
  String get navPractice => 'Luyện tập';

  @override
  String get navProfile => 'Cá nhân';

  @override
  String get commonOfflineBanner => 'Bạn đang ngoại tuyến';

  @override
  String get authBackButton => 'Quay lại';

  @override
  String get authOrDivider => 'hoặc';

  @override
  String get authContinueWithGoogle => 'Tiếp tục với Google';

  @override
  String get authGenericError => 'Đã xảy ra lỗi. Vui lòng thử lại.';

  @override
  String get authPhoneRequired => 'Vui lòng nhập số điện thoại.';

  @override
  String get authPhoneInvalid => 'Số điện thoại Việt Nam không hợp lệ.';

  @override
  String get authPasswordRequired => 'Vui lòng nhập mật khẩu.';

  @override
  String get authPasswordTooShort => 'Mật khẩu phải có ít nhất 8 ký tự.';

  @override
  String get authPasswordComplexity =>
      'Mật khẩu cần có chữ hoa, chữ thường và chữ số.';

  @override
  String get authDisplayNameRequired => 'Vui lòng nhập tên hiển thị.';

  @override
  String get authDisplayNameTooShort => 'Tên hiển thị phải có ít nhất 2 ký tự.';

  @override
  String get authDisplayNameTooLong =>
      'Tên hiển thị không được vượt quá 150 ký tự.';

  @override
  String get authConfirmPasswordRequired => 'Vui lòng xác nhận mật khẩu.';

  @override
  String get authConfirmPasswordMismatch => 'Mật khẩu xác nhận không khớp.';

  @override
  String get authForgotTitleReset => 'Đặt lại mật khẩu';

  @override
  String get authForgotTitleVerify => 'Xác minh mã';

  @override
  String get authForgotTitleCreate => 'Tạo mật khẩu';

  @override
  String get authForgotSubtitlePhone =>
      'Nhập số điện thoại của bạn, chúng tôi sẽ gửi mã để đặt lại mật khẩu.';

  @override
  String authForgotSubtitleOtp(String phone) {
    return 'Nhập mã 6 chữ số đã gửi đến $phone.';
  }

  @override
  String get authForgotSubtitlePassword =>
      'Tạo mật khẩu mới cho tài khoản của bạn.';

  @override
  String authStepProgress(int current, int total) {
    return 'Bước $current/$total';
  }

  @override
  String get authEmailOrPhoneLabel => 'Email hoặc số điện thoại';

  @override
  String get authSendResetCodeButton => 'Gửi mã đặt lại';

  @override
  String get authOtpMaxAttemptsReached =>
      'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.';

  @override
  String get authOtpVerifiedOnSave =>
      'Mã OTP sẽ được kiểm tra khi bạn lưu mật khẩu mới.';

  @override
  String get authResendCode => 'Gửi lại mã';

  @override
  String authResendInSeconds(int seconds) {
    return 'Gửi lại sau ${seconds}s';
  }

  @override
  String get authChangePhoneNumber => 'Đổi số điện thoại';

  @override
  String get authNewPasswordLabel => 'Mật khẩu mới';

  @override
  String get authConfirmNewPasswordLabel => 'Xác nhận mật khẩu mới';

  @override
  String get authSaveNewPasswordButton => 'Lưu mật khẩu mới';

  @override
  String get authEnterOtpAgain => 'Nhập lại mã OTP';

  @override
  String get authShowPassword => 'Hiện mật khẩu';

  @override
  String get authHidePassword => 'Ẩn mật khẩu';

  @override
  String get authOtpResentMessage => 'Đã gửi lại mã OTP.';

  @override
  String get authPasswordChangedMessage => 'Đã đổi mật khẩu.';

  @override
  String get authSignInTitle => 'Đăng nhập';

  @override
  String get authWelcomeBackSubtitle => 'Chào mừng bạn quay lại VocaNova';

  @override
  String get authPhoneNumberLabel => 'Số điện thoại';

  @override
  String get authPasswordLabel => 'Mật khẩu';

  @override
  String get authForgotPasswordLink => 'Quên mật khẩu?';

  @override
  String get authNewHerePrefix => 'Bạn chưa có tài khoản? ';

  @override
  String get authCreateAccountTitle => 'Tạo tài khoản';

  @override
  String get authVerifyEmailTitle => 'Xác minh email của bạn';

  @override
  String authOtpSubtitle(String phone) {
    return 'Nhập mã 6 chữ số đã gửi đến\n$phone';
  }

  @override
  String get authVerifyButton => 'Xác minh';

  @override
  String authAttemptsRemaining(int count) {
    return 'Bạn còn $count lần thử.';
  }

  @override
  String get authDidntReceiveCodePrefix => 'Bạn chưa nhận được mã? ';

  @override
  String get authOtpVerifiedSuccessMessage => 'Xác minh OTP thành công.';

  @override
  String get authRegistrationDataMissing => 'Thiếu dữ liệu đăng ký.';

  @override
  String get authStartLearningSubtitle => 'Bắt đầu học ngay hôm nay';

  @override
  String get authFullNameLabel => 'Họ và tên';

  @override
  String get authPasswordHintMinChars => 'Ít nhất 8 ký tự';

  @override
  String get authConfirmPasswordLabel => 'Xác nhận mật khẩu';

  @override
  String get authRepeatPasswordHint => 'Nhập lại mật khẩu';

  @override
  String get authAlreadyHaveAccountPrefix => 'Bạn đã có tài khoản? ';

  @override
  String get authLearningProfileSectionTitle =>
      'Cá nhân hóa gợi ý học (không bắt buộc)';

  @override
  String get authLearningProfileSectionSubtitle =>
      'Giúp VocaNova gợi ý chủ đề và từ vựng phù hợp ngay từ đầu.';

  @override
  String get authRegionLabel => 'Khu vực';

  @override
  String get authOccupationLabel => 'Nghề nghiệp';

  @override
  String get authEducationLevelLabel => 'Trình độ học vấn';

  @override
  String get authDateOfBirthLabel => 'Ngày sinh';

  @override
  String get authSelectDateOfBirth => 'Chọn ngày sinh';

  @override
  String get authOnboardingTitle => 'Thiết lập học tập';

  @override
  String get authSkipAction => 'Bỏ qua';

  @override
  String get authOnboardingGoalHeadline => 'Mục tiêu học từ vựng của bạn?';

  @override
  String get authOnboardingTopicsHeadline => 'Bạn quan tâm chủ đề nào?';

  @override
  String get authOnboardingGoalSubtitle =>
      'VocaNova sẽ ưu tiên nội dung theo mục tiêu này.';

  @override
  String get authOnboardingTopicsSubtitle =>
      'Chọn ít nhất một chủ đề để nhận gợi ý từ vựng phù hợp.';

  @override
  String get authOnboardingFinishButton => 'Hoàn tất';

  @override
  String get authOnboardingContinueButton => 'Tiếp tục';

  @override
  String get authCatalogLoadError =>
      'Không tải được danh mục. Vui lòng thử lại.';

  @override
  String get authRetryButton => 'Thử lại';

  @override
  String get authLearningProfileSaveFailed =>
      'Không thể lưu thiết lập học tập.';

  @override
  String get authLearningProfileSaveFailedRetry =>
      'Không thể lưu thiết lập học tập. Vui lòng thử lại.';

  @override
  String get authApiResponseInvalid =>
      'Dữ liệu phản hồi từ máy chủ không hợp lệ.';

  @override
  String get authGoogleTokenMissing =>
      'Google không trả về mã đăng nhập hợp lệ.';

  @override
  String get authGoogleClientIdMissing =>
      'Google Login chưa được cấu hình. Hãy chạy Flutter với --dart-define=GOOGLE_SERVER_CLIENT_ID=WEB_CLIENT_ID_CỦA_BẠN.';

  @override
  String get authGoogleClientConfigurationError =>
      'Cấu hình Google Login không hợp lệ. Hãy kiểm tra package Android, SHA-1 của signing key và Web Client ID.';

  @override
  String get authGoogleProviderConfigurationError =>
      'Google Play services hoặc nhà cung cấp Google chưa sẵn sàng hay cấu hình sai trên thiết bị này.';

  @override
  String get authGoogleUiUnavailable =>
      'Google không thể mở màn hình chọn tài khoản. Hãy mở lại ứng dụng và thử lại.';

  @override
  String get authGoogleInterrupted =>
      'Quá trình đăng nhập Google bị gián đoạn. Vui lòng thử lại.';

  @override
  String get authGoogleCanceled =>
      'Đăng nhập Google đã bị hủy. Nếu bạn đã chọn tài khoản trước khi thấy thông báo này, hãy kiểm tra package Android, SHA-1 và Web Client ID.';

  @override
  String authGoogleUnknownError(String details) {
    return 'Đăng nhập Google thất bại: $details';
  }

  @override
  String get dictBackTooltip => 'Quay lại';

  @override
  String get dictSearchHint => 'Tìm kiếm từ vựng...';

  @override
  String get dictClearSearchTooltip => 'Xóa tìm kiếm';

  @override
  String get dictAllLevelsLabel => 'Tất cả cấp độ';

  @override
  String get dictAllTopicsLabel => 'Tất cả chủ đề';

  @override
  String get dictSearchOfflineBanner =>
      'Ngoại tuyến — chỉ tìm trong từ đã lưu và lịch sử gần đây.';

  @override
  String get dictRecentSectionTitle => 'Gần đây';

  @override
  String get dictClearAction => 'Xóa';

  @override
  String get dictBrowseByTopicTitle => 'Khám phá theo chủ đề';

  @override
  String get dictSeeAllAction => 'Xem tất cả';

  @override
  String get dictRecentSearchesEmpty =>
      'Các tìm kiếm gần đây của bạn sẽ xuất hiện ở đây.';

  @override
  String dictWordCountLabel(int count) {
    return '$count từ';
  }

  @override
  String get dictNoMatchingCachedWords => 'Không tìm thấy từ đã lưu phù hợp';

  @override
  String get dictNoMatchingWords => 'Không tìm thấy từ phù hợp';

  @override
  String get dictReconnectHint => 'Kết nối lại để tìm kiếm toàn bộ từ điển.';

  @override
  String get dictAdjustFiltersHint =>
      'Hãy thử cách viết khác hoặc điều chỉnh bộ lọc.';

  @override
  String get dictTopicsTitle => 'Chủ đề';

  @override
  String get dictSearchTopicsHint => 'Tìm kiếm chủ đề...';

  @override
  String get dictNoPersonalTopicsMatch =>
      'Không có chủ đề cá nhân nào khớp với tìm kiếm của bạn.';

  @override
  String get dictNoSystemTopics => 'Không tìm thấy chủ đề hệ thống.';

  @override
  String get dictSystemLibraryLabel => 'Thư viện hệ thống';

  @override
  String get dictMyTopicsLabel => 'Chủ đề của tôi';

  @override
  String get dictPersonalModeNote =>
      'Chỉ những từ bạn đã lưu mới xuất hiện ở đây, sẵn sàng để luyện tập.';

  @override
  String get dictSystemModeNote =>
      'Khám phá tất cả các từ được đội ngũ VocaNova tổ chức.';

  @override
  String dictPersonalWordCount(int count) {
    return '$count từ cá nhân';
  }

  @override
  String dictSystemWordCount(int count) {
    return '$count từ hệ thống';
  }

  @override
  String get dictUnableToLoadTopics => 'Không thể tải chủ đề.';

  @override
  String get dictTryAgain => 'Thử lại';

  @override
  String get dictCategoryAll => 'Tất cả';

  @override
  String get dictCategoryEducation => 'Giáo dục';

  @override
  String get dictCategoryWork => 'Công việc';

  @override
  String get dictCategoryTravel => 'Du lịch';

  @override
  String get dictCategoryDailyLife => 'Đời sống hằng ngày';

  @override
  String dictMyTopicTitle(String name) {
    return '$name của tôi';
  }

  @override
  String get dictTopicFallbackName => 'chủ đề';

  @override
  String get dictTopicDetailFallbackTitle => 'Chi tiết chủ đề';

  @override
  String get dictUnableToLoadWordsRetry => 'Không thể tải từ vựng. Thử lại';

  @override
  String get dictPracticeSavedWords => 'Luyện tập từ đã lưu';

  @override
  String get dictNoWordsInCategory => 'Không có từ nào trong danh mục này.';

  @override
  String get dictRemovedFromTopic => 'Đã xóa khỏi chủ đề của bạn.';

  @override
  String get dictUnableToRemoveWord => 'Không thể xóa từ này.';

  @override
  String get dictAddToListTooltip => 'Thêm vào danh sách';

  @override
  String get dictRemoveFromTopicTooltip => 'Xóa khỏi chủ đề của tôi';

  @override
  String get dictStatMastered => 'Đã thành thạo';

  @override
  String get dictStatLearning => 'Đang học';

  @override
  String get dictStatNew => 'Mới';

  @override
  String get dictStatAvgMastery => 'Mức độ thành thạo TB';

  @override
  String get dictWordNotFound => 'Không tìm thấy từ.';

  @override
  String get dictDefinitionLabel => 'Định nghĩa';

  @override
  String get dictVietnameseMeaningLabel => 'Tiếng Việt';

  @override
  String get dictUnableToPlayAudio => 'Không thể phát âm thanh.';

  @override
  String get dictWordCopied => 'Đã sao chép từ vào bộ nhớ tạm.';

  @override
  String get dictShareTooltip => 'Chia sẻ';

  @override
  String get dictSavedTooltip => 'Đã lưu';

  @override
  String get dictSaveWordTooltip => 'Lưu từ';

  @override
  String get dictExampleLabel => 'Ví dụ';

  @override
  String get dictSynonymsLabel => 'Từ đồng nghĩa';

  @override
  String get dictAntonymsLabel => 'Từ trái nghĩa';

  @override
  String get dictPracticeLabel => 'Luyện tập';

  @override
  String get dictWordDetailOfflineBanner =>
      'Ngoại tuyến — đang hiển thị chi tiết từ đã lưu.';

  @override
  String get dictAddToListSheetSubtitle =>
      'Chọn một danh sách cá nhân hoặc tạo chủ đề sẵn sàng để làm quiz.';

  @override
  String get dictMyListsLabel => 'Danh sách của tôi';

  @override
  String get dictNewListLabel => 'Danh sách mới';

  @override
  String get dictNoteOptionalLabel => 'Ghi chú (không bắt buộc)';

  @override
  String get dictAddNoteHint => 'Thêm ghi chú...';

  @override
  String get dictSaveToListLabel => 'Lưu vào danh sách';

  @override
  String get dictAddToTopicLabel => 'Thêm vào chủ đề';

  @override
  String get dictCreateListPrompt => 'Tạo một danh sách để lưu từ này.';

  @override
  String get dictNoSystemTopicAssignment =>
      'Từ này chưa được gán vào chủ đề hệ thống nào.';

  @override
  String get dictAlreadyInTopic => 'Đã có trong chủ đề của bạn';

  @override
  String get dictUnableToLoadDestinations => 'Không thể tải nơi lưu của bạn.';

  @override
  String get dictListNameHint => 'Tên danh sách';

  @override
  String get dictCancelLabel => 'Hủy';

  @override
  String get dictCreateLabel => 'Tạo';

  @override
  String get dictUnableToCreateList => 'Không thể tạo danh sách.';

  @override
  String dictAddedToDestination(String name) {
    return 'Đã thêm vào $name.';
  }

  @override
  String get dictUnableToSaveWord => 'Không thể lưu từ này.';

  @override
  String get dictNoSavedWordDataError => 'Không có dữ liệu từ đã lưu.';

  @override
  String get dictWordDetailLoadError => 'Không thể tải chi tiết từ.';

  @override
  String get dictSearchRefreshError =>
      'Không thể tải dữ liệu mới. Đang hiển thị từ đã lưu.';

  @override
  String get listsTitle => 'Danh sách từ';

  @override
  String get listsCreateDialogTitle => 'Tạo danh sách';

  @override
  String get listsRenameDialogTitle => 'Đổi tên danh sách';

  @override
  String get listsMyListsSection => 'Danh sách của tôi';

  @override
  String get listsPersonalTopicsSection => 'Chủ đề cá nhân';

  @override
  String get listsNameFieldHint => 'Tên danh sách';

  @override
  String get listsCancel => 'Hủy';

  @override
  String get listsCreateAction => 'Tạo';

  @override
  String get listsSaveAction => 'Lưu';

  @override
  String get listsRenameAction => 'Đổi tên';

  @override
  String get listsDeleteAction => 'Xóa';

  @override
  String get listsDeleteConfirmTitle => 'Xóa danh sách?';

  @override
  String listsDeleteConfirmBody(String name) {
    return 'Danh sách \"$name\" sẽ bị xóa.';
  }

  @override
  String get listsNameRequiredError => 'Vui lòng nhập tên danh sách.';

  @override
  String get listsNameMaxLengthError => 'Tên danh sách tối đa 100 ký tự.';

  @override
  String listsWordCount(int count) {
    return '$count từ';
  }

  @override
  String listsCreatedOnLabel(String date) {
    return 'Tạo ngày $date';
  }

  @override
  String get listsOfflineBanner =>
      'Bạn đang offline. Đang hiển thị danh sách đã lưu.';

  @override
  String get listsEmptyState =>
      'Bạn chưa có danh sách từ nào.\nNhấn + để tạo danh sách đầu tiên.';

  @override
  String get listsDetailTitleFallback => 'Chi tiết danh sách';

  @override
  String get listsAddWordAction => 'Thêm từ';

  @override
  String get listsDetailEmpty => 'Danh sách chưa có từ nào.';

  @override
  String get listsRemoveWordConfirmTitle => 'Xóa từ?';

  @override
  String listsRemoveWordConfirmBody(String word) {
    return 'Xóa \"$word\" khỏi danh sách?';
  }

  @override
  String get listsLoadTopicsError => 'Không thể tải chủ đề.';

  @override
  String get listsAddRandomDialogTitle => 'Thêm từ ngẫu nhiên';

  @override
  String get listsByTopicOption => 'Theo chủ đề';

  @override
  String get listsSynonymOption => 'Từ đồng nghĩa';

  @override
  String get listsAntonymOption => 'Từ trái nghĩa';

  @override
  String get listsCountFieldLabel => 'Số lượng (1-50)';

  @override
  String get listsAddAction => 'Thêm';

  @override
  String get listsAddRandomAction => 'Thêm ngẫu nhiên';

  @override
  String get listsStartQuizAction => 'Bắt đầu kiểm tra';

  @override
  String listsCorrectCount(int count) {
    return 'Đúng: $count';
  }

  @override
  String listsWrongCount(int count) {
    return 'Sai: $count';
  }

  @override
  String listsNoteLabel(String note) {
    return 'Ghi chú: $note';
  }

  @override
  String get listsDetailOfflineBanner =>
      'Bạn đang offline. Đang hiển thị các từ đã lưu.';

  @override
  String get listsSearchWordHint => 'Tìm từ tiếng Anh';

  @override
  String get listsLoadListsError => 'Không thể tải danh sách từ.';

  @override
  String get listsCreateError =>
      'Không thể tạo danh sách. Tên có thể đã tồn tại.';

  @override
  String get listsRenameError => 'Không thể đổi tên danh sách.';

  @override
  String get listsDeleteError => 'Không thể xóa danh sách.';

  @override
  String get listsOfflineMutateError =>
      'Cần kết nối mạng để thay đổi danh sách.';

  @override
  String get listsLoadWordsError => 'Không thể tải từ trong danh sách.';

  @override
  String get listsLoadMoreWordsError => 'Không thể tải thêm từ.';

  @override
  String get listsAddWordError => 'Không thể thêm từ vào danh sách.';

  @override
  String get listsAddRandomError => 'Không thể thêm từ ngẫu nhiên.';

  @override
  String get listsRemoveWordError => 'Không thể xóa từ khỏi danh sách.';

  @override
  String get progressOverviewTitle => 'Tiến độ học tập';

  @override
  String get progressChartsTooltip => 'Biểu đồ chi tiết';

  @override
  String progressStreakDaysLabel(int days) {
    return '$days ngày liên tiếp';
  }

  @override
  String progressLongestStreakLabel(int days) {
    return 'Kỷ lục: $days ngày';
  }

  @override
  String get progressAccuracy7DaysLabel => 'Độ chính xác 7 ngày';

  @override
  String progressCorrectAnswersLabel(int correct, int total) {
    return '$correct/$total câu đúng';
  }

  @override
  String get progressWordsInProgressLabel => 'Từ đang học';

  @override
  String get progressMasteredWordsLabel => 'Từ đã thành thạo';

  @override
  String get progressSessionsThisMonthLabel => 'Bài kiểm tra tháng này';

  @override
  String get progressOfflineBanner =>
      'Bạn đang ngoại tuyến. Đang hiển thị dữ liệu tiến độ đã lưu.';

  @override
  String get progressNoDataMessage => 'Chưa có dữ liệu tiến độ.';

  @override
  String get progressRetry => 'Thử lại';

  @override
  String get progressChartsTitle => 'Biểu đồ tiến độ';

  @override
  String get progressGranularityDaily => 'Theo ngày';

  @override
  String get progressGranularityWeekly => 'Theo tuần';

  @override
  String get progressGranularityMonthly => 'Theo tháng';

  @override
  String get progressSessionsCountLabel => 'Số buổi học';

  @override
  String get progressMasteryLevelLabel => 'Mức độ thành thạo';

  @override
  String get progressTop10WeakestWordsLabel => '10 từ yếu nhất';

  @override
  String get progressNoWeakestWords => 'Chưa có từ yếu cần ôn tập.';

  @override
  String progressMasteryLevelShort(int level) {
    return 'Lv.$level';
  }

  @override
  String progressWordStatsLabel(int correct, int wrong) {
    return 'Đúng $correct · Sai $wrong';
  }

  @override
  String get progressNoCachedDataError => 'Không có dữ liệu tiến độ đã lưu.';

  @override
  String get progressLoadOverviewError => 'Không thể tải tổng quan tiến độ.';

  @override
  String get progressLoadChartsError => 'Không thể tải biểu đồ tiến độ.';

  @override
  String get progressChangeGranularityError =>
      'Không thể đổi khoảng thời gian biểu đồ.';

  @override
  String get homeWelcomeBack => 'Chào mừng trở lại';

  @override
  String homeGreetingName(String name) {
    return 'Chào, $name';
  }

  @override
  String get homeGreetingMorning => 'Chào buổi sáng';

  @override
  String get homeGreetingAfternoon => 'Chào buổi chiều';

  @override
  String get homeGreetingEvening => 'Chào buổi tối';

  @override
  String get homeSeeAll => 'Xem tất cả';

  @override
  String get homeSearchHint => 'Tìm một từ...';

  @override
  String get homeDailyGoalLabel => 'MỤC TIÊU HÀNG NGÀY';

  @override
  String homeGoalProgress(int mastered, int total) {
    return '$mastered / $total từ';
  }

  @override
  String get homeMasteredSoFar => 'đã thành thạo';

  @override
  String homeStreakActive(int days) {
    return 'Duy trì chuỗi $days ngày';
  }

  @override
  String get homeStreakInactive => 'Bắt đầu chuỗi hôm nay';

  @override
  String get homeWordOfTheDayLabel => 'TỪ VỰNG HÔM NAY';

  @override
  String get homeLearnThisWord => 'Học từ này';

  @override
  String get homeDailyWordLoading => 'Đang tải…';

  @override
  String get homeDailyWordUnavailable => 'Chưa có từ';

  @override
  String get homeDailyWordChoosing => 'Đang chọn một từ trong từ điển…';

  @override
  String get homeDailyWordLoadError =>
      'Không thể tải từ hôm nay. Kéo xuống để thử lại.';

  @override
  String get homePronunciationPlayTooltip => 'Phát cách đọc';

  @override
  String get homePronunciationPlayError => 'Không thể phát cách đọc.';

  @override
  String get homeStatWords => 'Từ vựng';

  @override
  String get homeStatAccuracy => 'Độ chính xác';

  @override
  String get homeStatMastered => 'Đã thành thạo';

  @override
  String get homeContinueLabel => 'TIẾP TỤC';

  @override
  String homeWordCount(int count) {
    return '$count từ';
  }

  @override
  String get homeQuickActionsTitle => 'Thao tác nhanh';

  @override
  String get homeActionQuiz => 'Kiểm tra';

  @override
  String get homeActionReview => 'Ôn tập';

  @override
  String get homeActionTopics => 'Chủ đề';

  @override
  String get homeTopicsForYouTitle => 'Chủ đề dành cho bạn';

  @override
  String get homeTopicsForYouEmpty =>
      'Bộ từ tiếp theo của bạn đang được chuẩn bị';

  @override
  String get homeExploreTopics => 'Khám phá chủ đề';

  @override
  String homeTopicWordCount(int count) {
    return '$count từ';
  }

  @override
  String homeWordsToReviewLabel(String count) {
    return '$count từ cần ôn tập';
  }

  @override
  String get homeTapToReviewMistakes => 'Nhấn để ôn lại các lỗi sai';

  @override
  String get homeMyListsTitle => 'Danh sách của tôi';

  @override
  String get homeCreateFirstList => 'Tạo danh sách đầu tiên để bắt đầu học';

  @override
  String get homeWeekdayMonShort => 'T2';

  @override
  String get homeWeekdayTueShort => 'T3';

  @override
  String get homeWeekdayWedShort => 'T4';

  @override
  String get homeWeekdayThuShort => 'T5';

  @override
  String get homeWeekdayFriShort => 'T6';

  @override
  String get homeWeekdaySatShort => 'T7';

  @override
  String get homeWeekdaySunShort => 'CN';

  @override
  String get notifTitle => 'Thông báo';

  @override
  String get notifMarkAllRead => 'Đọc tất cả';

  @override
  String get notifEmptyMessage => 'Chưa có thông báo nào.';

  @override
  String get notifRetry => 'Thử lại';

  @override
  String get notifJustNow => 'Vừa xong';

  @override
  String notifMinutesAgo(int count) {
    return '$count phút trước';
  }

  @override
  String notifHoursAgo(int count) {
    return '$count giờ trước';
  }

  @override
  String notifDaysAgo(int count) {
    return '$count ngày trước';
  }

  @override
  String get notifLoadError => 'Không thể tải thông báo.';

  @override
  String get quizConfigTitle => 'Cấu hình kiểm tra';

  @override
  String get quizConfigScopeSection => 'Phạm vi từ';

  @override
  String get quizConfigScopeAll => 'Tất cả';

  @override
  String get quizConfigScopeFromDate => 'Từ ngày';

  @override
  String get quizConfigScopeToDate => 'Đến ngày';

  @override
  String get quizConfigScopeDateRange => 'Khoảng ngày';

  @override
  String get quizConfigScopeWrongWords => 'Từ sai gần đây';

  @override
  String get quizConfigDateFrom => 'Ngày bắt đầu';

  @override
  String get quizConfigDateTo => 'Ngày kết thúc';

  @override
  String get quizConfigSourceSection => 'Nguồn kiểm tra';

  @override
  String get quizConfigModeSection => 'Chế độ';

  @override
  String get quizConfigQuestionTypeSection => 'Loại câu hỏi';

  @override
  String get quizConfigAnswerMethodSection => 'Cách trả lời';

  @override
  String get quizConfigWordOrderSection => 'Thứ tự';

  @override
  String get quizConfigQuestionLimitSection => 'Số câu hỏi';

  @override
  String get quizConfigNeedConnection => 'Cần kết nối mạng';

  @override
  String get quizConfigStartButton => 'Bắt đầu';

  @override
  String get quizConfigQuestionTypeWordToMeaning => 'Từ → nghĩa';

  @override
  String get quizConfigQuestionTypeMeaningToWord => 'Nghĩa → từ';

  @override
  String get quizConfigQuestionTypeDescToWord => 'Mô tả → từ';

  @override
  String get quizAnswerMultipleChoice => 'Trắc nghiệm';

  @override
  String get quizAnswerTyping => 'Gõ đáp án';

  @override
  String get quizAnswerAiTyping => 'Gõ (AI chấm)';

  @override
  String get quizConfigOrderRandom => 'Ngẫu nhiên';

  @override
  String get quizConfigOrderNewest => 'Mới nhất';

  @override
  String get quizConfigOrderOldest => 'Cũ nhất';

  @override
  String get quizConfigOrderByDifficulty => 'Theo độ khó';

  @override
  String get quizModeStandard => 'Tiêu chuẩn';

  @override
  String get quizModeTimed => 'Tính giờ';

  @override
  String get quizModeChallenge => 'Thử thách';

  @override
  String get quizModeElimination => 'Loại trực tiếp';

  @override
  String get quizConfigModeStandardSubtitle => 'Theo nhịp độ của bạn';

  @override
  String get quizConfigModeTimedSubtitle => 'Chạy đua thời gian';

  @override
  String get quizConfigModeChallengeSubtitle => 'Mạng và chuỗi đúng';

  @override
  String get quizConfigModeEliminationSubtitle => 'Sai là kết thúc';

  @override
  String get quizConfigTimeLimitLabel => 'Thời gian (giây)';

  @override
  String get quizConfigLivesLabel => 'Số mạng';

  @override
  String get quizConfigLimitAll => 'Tất cả';

  @override
  String get quizConfigLimitCustom => 'Tùy chỉnh';

  @override
  String get quizConfigCustomLimitLabel => 'Số câu hỏi mong muốn';

  @override
  String quizConfigCustomLimitHelper(int wordCount) {
    return 'Nguồn hiện có $wordCount từ · nhập lớn hơn sẽ lấy tối đa $wordCount';
  }

  @override
  String get quizConfigCustomLimitHelperEmpty => 'Nhập số câu hỏi mong muốn';

  @override
  String get quizConfigSourceMyList => 'Danh sách của tôi';

  @override
  String get quizConfigSourcePersonalTopic => 'Chủ đề cá nhân';

  @override
  String get quizConfigSourceHintList => 'Chọn một danh sách từ của bạn.';

  @override
  String get quizConfigSourceHintTopic =>
      'Chọn một chủ đề chứa các từ bạn đã lưu.';

  @override
  String get quizConfigSourceNoWords => 'Chưa có từ để kiểm tra';

  @override
  String quizConfigSourceWordCount(int count) {
    return '$count từ';
  }

  @override
  String get quizConfigEmptyTopicHint =>
      'Hãy lưu từ vào một chủ đề cá nhân trước khi kiểm tra.';

  @override
  String get quizConfigEmptyListHint =>
      'Hãy tạo danh sách và thêm từ trước khi kiểm tra.';

  @override
  String get quizConfigSummaryMode => 'Chế độ';

  @override
  String get quizConfigSummaryAnswer => 'Trả lời';

  @override
  String get quizConfigSummaryCount => 'Số câu';

  @override
  String get quizConfigLoadSourcesError => 'Không thể tải nguồn kiểm tra.';

  @override
  String get quizConfigValidateNoSource =>
      'Vui lòng chọn danh sách hoặc chủ đề cá nhân để kiểm tra.';

  @override
  String get quizConfigValidateNoQuestionCount => 'Vui lòng nhập số câu hỏi.';

  @override
  String get quizConfigValidateQuestionCountPositive =>
      'Số câu hỏi phải lớn hơn 0.';

  @override
  String get quizConfigValidateDateFromRequired =>
      'Vui lòng chọn ngày bắt đầu.';

  @override
  String get quizConfigValidateDateToRequired => 'Vui lòng chọn ngày kết thúc.';

  @override
  String get quizConfigValidateDateOrder =>
      'Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.';

  @override
  String get quizConfigValidateTimedPositive =>
      'Chế độ tính giờ cần thời gian lớn hơn 0.';

  @override
  String get quizConfigValidateEliminationPositive =>
      'Chế độ loại trực tiếp cần số mạng lớn hơn 0.';

  @override
  String get quizConfigCreateSessionError =>
      'Không thể tạo bài kiểm tra. Hãy kiểm tra số lượng từ.';

  @override
  String get quizSessionTitle => 'Bài kiểm tra';

  @override
  String get quizSessionAbandonTooltip => 'Bỏ bài';

  @override
  String quizSessionQuestionNumber(int number) {
    return 'Câu $number';
  }

  @override
  String get quizSessionFinishing => 'Đang kết thúc...';

  @override
  String get quizSessionViewResult => 'Xem kết quả';

  @override
  String get quizSessionNext => 'Tiếp theo';

  @override
  String get quizSessionAbandonDialogTitle => 'Bỏ bài kiểm tra?';

  @override
  String get quizSessionAbandonDialogContent =>
      'Tiến độ hiện tại sẽ được kết thúc và lưu lại.';

  @override
  String get quizSessionAbandonCancel => 'Tiếp tục làm';

  @override
  String get quizSessionAbandonConfirm => 'Bỏ bài';

  @override
  String quizSessionProgressLabel(int current, int total) {
    return 'Câu $current/$total';
  }

  @override
  String get quizSessionUnavailableMessage =>
      'Không thể khôi phục bài kiểm tra đang chạy. Vui lòng tạo một bài kiểm tra mới.';

  @override
  String get quizSessionCreateNew => 'Tạo bài kiểm tra';

  @override
  String get quizTypingLabelAi => 'Nhập câu trả lời của bạn';

  @override
  String get quizTypingLabelDefault => 'Nhập đáp án';

  @override
  String get quizTypingHelperAi =>
      'AI sẽ đánh giá mức độ chính xác về ý nghĩa.';

  @override
  String get quizTypingHelperDefault =>
      'Không phân biệt hoa thường và dấu câu cuối.';

  @override
  String get quizTypingAiEvaluating => 'AI đang đánh giá...';

  @override
  String get quizTypingSubmit => 'Gửi câu trả lời';

  @override
  String get quizTypingEmptyAnswer => 'Vui lòng nhập câu trả lời.';

  @override
  String get quizTypingCorrect => 'Chính xác';

  @override
  String get quizTypingIncorrect => 'Chưa chính xác';

  @override
  String quizTypingExpectedAnswer(String answer) {
    return 'Đáp án: $answer';
  }

  @override
  String quizTypingAiScore(int score) {
    return 'Điểm AI: $score%';
  }

  @override
  String quizTypingAiSuggestion(String suggestion) {
    return 'Gợi ý: $suggestion';
  }

  @override
  String get quizSessionSubmitError =>
      'Không thể gửi câu trả lời. Vui lòng thử lại.';

  @override
  String get quizSessionFinishError =>
      'Không thể kết thúc bài kiểm tra. Vui lòng thử lại.';

  @override
  String get quizResultHeadlineGreat => 'Tuyệt vời!';

  @override
  String get quizResultHeadlineGood => 'Làm tốt lắm!';

  @override
  String get quizResultHeadlineTryAgain => 'Cố gắng thêm nhé!';

  @override
  String quizResultSummary(int correct, int total, String duration) {
    return '$correct/$total câu đúng · $duration';
  }

  @override
  String get quizResultAccuracyLabel => 'chính xác';

  @override
  String get quizResultCorrectLabel => 'Đúng';

  @override
  String get quizResultWrongLabel => 'Sai';

  @override
  String get quizResultBestStreakLabel => 'Chuỗi tốt nhất';

  @override
  String get quizResultListTitle => 'KẾT QUẢ';

  @override
  String get quizResultYourAnswerLabel => 'Bạn trả lời';

  @override
  String get quizResultNoAnswer => 'Chưa trả lời';

  @override
  String get quizResultAnswerLabel => 'Đáp án';

  @override
  String get quizResultReviewWrongButton => 'Xem lại từ sai';

  @override
  String get quizResultRetryButton => 'Làm lại';

  @override
  String get quizResultDoneButton => 'Hoàn tất';

  @override
  String get quizResultLoadError => 'Không thể tải kết quả kiểm tra.';

  @override
  String get quizResultRetryLoadButton => 'Thử lại';

  @override
  String get quizResultInvalidSession => 'Mã bài kiểm tra không hợp lệ.';

  @override
  String get quizWrongWordsTitle => 'Từ trả lời sai';

  @override
  String get quizWrongWordsRetryButton => 'Test lại';

  @override
  String get quizWrongWordsEmpty => 'Bạn chưa có từ nào trong danh sách sai.';

  @override
  String get quizWrongWordsNoMeaning => 'Chưa có nghĩa';

  @override
  String quizWrongWordsStats(int correct, int wrong) {
    return 'Đúng: $correct · Sai: $wrong';
  }

  @override
  String quizWrongWordsMasteryLevel(int level) {
    return 'Lv.$level';
  }

  @override
  String get quizWrongWordsLoadError => 'Không thể tải danh sách từ sai.';

  @override
  String get quizWrongWordsLoadMoreError => 'Không thể tải thêm từ sai.';

  @override
  String get quizWrongWordsRemoveError => 'Không thể bỏ từ khỏi danh sách sai.';

  @override
  String get settingsSectionAppearance => 'GIAO DIỆN';

  @override
  String get settingsDarkMode => 'Chế độ tối';

  @override
  String get settingsDarkModeSubtitle => 'Chuyển sang giao diện tối';

  @override
  String get settingsFollowSystemTheme => 'Theo giao diện hệ thống';

  @override
  String get settingsSectionLanguage => 'NGÔN NGỮ';

  @override
  String get settingsAppLanguage => 'Ngôn ngữ ứng dụng';

  @override
  String get settingsSectionNotifications => 'THÔNG BÁO';

  @override
  String get settingsDailyReminder => 'Nhắc nhở hằng ngày';

  @override
  String get settingsDailyReminderSubtitle => 'Nhắc bạn học mỗi ngày';

  @override
  String get settingsStreakAlert => 'Cảnh báo chuỗi ngày học';

  @override
  String get settingsStreakAlertSubtitle =>
      'Cảnh báo trước khi chuỗi ngày học bị ngắt';

  @override
  String get settingsReviewDue => 'Đến hạn ôn tập';

  @override
  String get settingsReviewDueSubtitle =>
      'Từ vựng sẵn sàng để ôn tập theo chu kỳ';

  @override
  String get settingsSectionAudio => 'ÂM THANH';

  @override
  String get settingsAutoPlayPronunciation => 'Tự động phát âm';

  @override
  String get settingsAutoPlayPronunciationSubtitle =>
      'Tự động phát khi xem một từ';

  @override
  String get settingsSoundEffects => 'Hiệu ứng âm thanh';

  @override
  String get settingsSoundEffectsSubtitle =>
      'Âm thanh và phản hồi khi làm bài kiểm tra';

  @override
  String get settingsSectionAccount => 'TÀI KHOẢN';

  @override
  String get settingsPrivacyPolicy => 'Chính sách bảo mật';

  @override
  String get settingsDeleteAccount => 'Xóa tài khoản';

  @override
  String settingsVersionLabel(String version) {
    return 'VocaNova v$version';
  }

  @override
  String get settingsPrivacyPolicyBody =>
      'VocaNova chỉ lưu trữ thông tin tài khoản và dữ liệu học tập cần thiết để cung cấp tính năng luyện từ vựng, theo dõi tiến độ và đồng bộ hóa. Thông tin đăng nhập của bạn được bảo vệ và không bao giờ hiển thị trong ứng dụng.';

  @override
  String get settingsBackToProfile => 'Hồ sơ';

  @override
  String get settingsTitle => 'Cài đặt';

  @override
  String get settingsDeleteAccountDialogTitle => 'Xóa tài khoản?';

  @override
  String get settingsDeleteAccountDialogBody =>
      'Thao tác này sẽ xóa vĩnh viễn hồ sơ và dữ liệu học tập của bạn. Không thể hoàn tác.';

  @override
  String get settingsCancel => 'Hủy';

  @override
  String get settingsContinue => 'Tiếp tục';

  @override
  String get settingsDeleteAccountFailed =>
      'Không thể xóa tài khoản. Vui lòng thử lại.';

  @override
  String get settingsDone => 'Xong';

  @override
  String get profileSectionLearning => 'HỌC TẬP';

  @override
  String get profileMyVocabulary => 'Từ vựng của tôi';

  @override
  String get profileMyVocabularySubtitle => 'Đã sưu tầm 248 từ';

  @override
  String get profileStatistics => 'Thống kê';

  @override
  String get profileStatisticsSubtitle => 'Tiến độ & phân tích';

  @override
  String get profileTestHistory => 'Lịch sử kiểm tra';

  @override
  String get profileTestHistorySubtitle => 'Các buổi luyện tập trước đây';

  @override
  String get profileLearningGoals => 'Mục tiêu học tập';

  @override
  String get profileLearningGoalsSubtitle => 'Mục tiêu B2 → C1';

  @override
  String get profileSectionAccount => 'TÀI KHOẢN';

  @override
  String get profilePersonalInformation => 'Thông tin cá nhân';

  @override
  String get profilePersonalInformationSubtitle =>
      'Tên, ảnh đại diện, số điện thoại';

  @override
  String get profileNotifications => 'Thông báo';

  @override
  String get profileDailyRemindersOn => 'Nhắc nhở hằng ngày: Bật';

  @override
  String get profileDailyRemindersOff => 'Nhắc nhở hằng ngày: Tắt';

  @override
  String get profileLanguage => 'Ngôn ngữ';

  @override
  String get profileLanguageEnglish => 'Tiếng Anh';

  @override
  String get profileLanguageVietnamese => 'Tiếng Việt';

  @override
  String get profileTheme => 'Giao diện';

  @override
  String get profileThemeDark => 'Chế độ tối';

  @override
  String get profileThemeLight => 'Chế độ sáng';

  @override
  String get profileThemeSystem => 'Theo hệ thống';

  @override
  String get profileSectionApp => 'ỨNG DỤNG';

  @override
  String get profileSettingsMenuTitle => 'Cài đặt';

  @override
  String get profileSettingsMenuSubtitle => 'Âm thanh, bộ nhớ, đồng bộ';

  @override
  String get profilePrivacyData => 'Quyền riêng tư & dữ liệu';

  @override
  String get profilePrivacyDataSubtitle => 'Quản lý dữ liệu của bạn';

  @override
  String get profilePrivacyDataBody =>
      'VocaNova lưu trữ hồ sơ và tiến độ học tập của bạn để từ vựng luôn được đồng bộ giữa các phiên sử dụng.';

  @override
  String get profileHelpFeedback => 'Trợ giúp & phản hồi';

  @override
  String get profileHelpFeedbackSubtitle => 'Câu hỏi thường gặp & hỗ trợ';

  @override
  String get profileHelpFeedbackBody =>
      'Bạn cần trợ giúp? Hãy chia sẻ với đội ngũ hỗ trợ VocaNova về vấn đề gặp phải, màn hình bạn đang dùng và các bước dẫn đến lỗi.';

  @override
  String get profileSignOut => 'Đăng xuất';

  @override
  String get profileVersionLabel => 'VocaNova v1.0.0 · SEP490_19';

  @override
  String get profileUploadAvatarFailed => 'Không thể tải ảnh đại diện lên.';

  @override
  String get profileUpdateSuccess => 'Cập nhật hồ sơ thành công.';

  @override
  String get profileUpdateFailed => 'Không thể cập nhật hồ sơ.';

  @override
  String get profilePasswordChangeSuccess => 'Đổi mật khẩu thành công.';

  @override
  String get profilePasswordChangeFailed => 'Không thể đổi mật khẩu.';

  @override
  String get profileDone => 'Xong';

  @override
  String get profileSignOutConfirmTitle => 'Đăng xuất?';

  @override
  String get profileSignOutConfirmBody =>
      'Bạn sẽ cần đăng nhập lại để tiếp tục học.';

  @override
  String get profileCancel => 'Hủy';

  @override
  String get profilePhoneNotLinked => 'Chưa liên kết số điện thoại';

  @override
  String get profileLevelB2 => 'Trình độ B2';

  @override
  String profileStreakLabel(int days) {
    return 'Chuỗi $days ngày';
  }

  @override
  String get profileEditAction => 'Sửa';

  @override
  String get profileStatWords => 'Từ vựng';

  @override
  String get profileStatAccuracy => 'Độ chính xác';

  @override
  String get profileStatStreak => 'Chuỗi ngày';

  @override
  String get profileStatBadges => 'Huy hiệu';

  @override
  String get profileEditSubtitle => 'Cập nhật thông tin hồ sơ của bạn.';

  @override
  String get profileFieldPicture => 'Ảnh đại diện';

  @override
  String get profileChooseAvatar => 'Chọn một ảnh đại diện';

  @override
  String get profileFieldFullName => 'Họ và tên';

  @override
  String get profileNameHint => 'Nguyễn Văn An';

  @override
  String get profileNameTooShort => 'Tên phải có ít nhất 2 ký tự';

  @override
  String get profileFieldPhoneNumber => 'Số điện thoại';

  @override
  String get profilePhoneNotLinkedShort => 'Chưa liên kết';

  @override
  String get profileChangePassword => 'Đổi mật khẩu';

  @override
  String get profileSaveChanges => 'Lưu thay đổi';

  @override
  String get profileAvatarOpening => 'Đang mở...';

  @override
  String get profileChooseFromDevice => 'Chọn từ thiết bị';

  @override
  String get profileAvatarHint => 'JPG, PNG hoặc WebP · Tối đa 5MB';

  @override
  String get profileAvatarTooLarge =>
      'Ảnh đại diện phải nhỏ hơn hoặc bằng 5MB.';

  @override
  String get profilePhotoLibraryError => 'Không thể mở thư viện ảnh.';

  @override
  String get profileChangePasswordSubtitle =>
      'Sử dụng ít nhất 8 ký tự gồm chữ hoa, chữ thường và số.';

  @override
  String get profileFieldCurrentPassword => 'Mật khẩu hiện tại';

  @override
  String get profileCurrentPasswordHint => 'Nhập mật khẩu hiện tại';

  @override
  String get profileFieldNewPassword => 'Mật khẩu mới';

  @override
  String get profileNewPasswordHint => 'Ít nhất 8 ký tự';

  @override
  String get profileFieldConfirmPassword => 'Xác nhận mật khẩu mới';

  @override
  String get profileConfirmPasswordHint => 'Nhập lại mật khẩu';

  @override
  String get profileUpdatePassword => 'Cập nhật mật khẩu';

  @override
  String get profileClose => 'Đóng';

  @override
  String get profileHidePassword => 'Ẩn mật khẩu';

  @override
  String get profileShowPassword => 'Hiện mật khẩu';

  @override
  String get profileTryAgain => 'Thử lại';
}
