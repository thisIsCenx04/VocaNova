// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'VocaNova';

  @override
  String get navHome => 'Home';

  @override
  String get navSearch => 'Search';

  @override
  String get navLists => 'Lists';

  @override
  String get navPractice => 'Practice';

  @override
  String get navProfile => 'Profile';

  @override
  String get commonOfflineBanner => 'You\'re offline';

  @override
  String get authBackButton => 'Back';

  @override
  String get authOrDivider => 'or';

  @override
  String get authContinueWithGoogle => 'Continue with Google';

  @override
  String get authGenericError => 'Something went wrong. Please try again.';

  @override
  String get authPhoneRequired => 'Please enter your phone number.';

  @override
  String get authPhoneInvalid => 'Invalid Vietnamese phone number.';

  @override
  String get authPasswordRequired => 'Please enter your password.';

  @override
  String get authPasswordTooShort => 'Password must be at least 8 characters.';

  @override
  String get authPasswordComplexity =>
      'Password needs an uppercase letter, a lowercase letter, and a digit.';

  @override
  String get authDisplayNameRequired => 'Please enter a display name.';

  @override
  String get authDisplayNameTooShort =>
      'Display name must be at least 2 characters.';

  @override
  String get authDisplayNameTooLong =>
      'Display name must not exceed 150 characters.';

  @override
  String get authConfirmPasswordRequired => 'Please confirm your password.';

  @override
  String get authConfirmPasswordMismatch => 'Passwords do not match.';

  @override
  String get authForgotTitleReset => 'Reset password';

  @override
  String get authForgotTitleVerify => 'Verify your code';

  @override
  String get authForgotTitleCreate => 'Create password';

  @override
  String get authForgotSubtitlePhone =>
      'Enter your phone number and we\'ll send a code to reset your password.';

  @override
  String authForgotSubtitleOtp(String phone) {
    return 'Enter the 6-digit code sent to $phone.';
  }

  @override
  String get authForgotSubtitlePassword =>
      'Create a new password for your account.';

  @override
  String authStepProgress(int current, int total) {
    return 'Step $current/$total';
  }

  @override
  String get authEmailOrPhoneLabel => 'Email or phone number';

  @override
  String get authSendResetCodeButton => 'Send reset code';

  @override
  String get authOtpMaxAttemptsReached =>
      'You\'ve entered the wrong OTP more than 5 times. Please resend the code.';

  @override
  String get authOtpVerifiedOnSave =>
      'The OTP code will be checked when you save the new password.';

  @override
  String get authResendCode => 'Resend code';

  @override
  String authResendInSeconds(int seconds) {
    return 'Resend in ${seconds}s';
  }

  @override
  String get authChangePhoneNumber => 'Change phone number';

  @override
  String get authNewPasswordLabel => 'New password';

  @override
  String get authConfirmNewPasswordLabel => 'Confirm new password';

  @override
  String get authSaveNewPasswordButton => 'Save new password';

  @override
  String get authEnterOtpAgain => 'Enter OTP again';

  @override
  String get authShowPassword => 'Show password';

  @override
  String get authHidePassword => 'Hide password';

  @override
  String get authOtpResentMessage => 'OTP code resent.';

  @override
  String get authPasswordChangedMessage => 'Password changed.';

  @override
  String get authSignInTitle => 'Sign in';

  @override
  String get authWelcomeBackSubtitle => 'Welcome back to VocaNova';

  @override
  String get authPhoneNumberLabel => 'Phone number';

  @override
  String get authPasswordLabel => 'Password';

  @override
  String get authForgotPasswordLink => 'Forgot password?';

  @override
  String get authNewHerePrefix => 'New here? ';

  @override
  String get authCreateAccountTitle => 'Create account';

  @override
  String get authVerifyEmailTitle => 'Verify your email';

  @override
  String authOtpSubtitle(String phone) {
    return 'Enter the 6-digit code sent to\n$phone';
  }

  @override
  String get authVerifyButton => 'Verify';

  @override
  String authAttemptsRemaining(int count) {
    return 'You have $count attempts remaining.';
  }

  @override
  String get authDidntReceiveCodePrefix => 'Didn\'t receive it? ';

  @override
  String get authOtpVerifiedSuccessMessage => 'OTP verified successfully.';

  @override
  String get authRegistrationDataMissing => 'Missing registration data.';

  @override
  String get authStartLearningSubtitle => 'Start learning today';

  @override
  String get authFullNameLabel => 'Full name';

  @override
  String get authPasswordHintMinChars => 'At least 8 characters';

  @override
  String get authConfirmPasswordLabel => 'Confirm password';

  @override
  String get authRepeatPasswordHint => 'Repeat your password';

  @override
  String get authAlreadyHaveAccountPrefix => 'Already have an account? ';

  @override
  String get authLearningProfileSectionTitle =>
      'Personalize learning suggestions (optional)';

  @override
  String get authLearningProfileSectionSubtitle =>
      'Help VocaNova suggest topics and vocabulary that fit you from day one.';

  @override
  String get authRegionLabel => 'Region';

  @override
  String get authOccupationLabel => 'Occupation';

  @override
  String get authEducationLevelLabel => 'Education level';

  @override
  String get authDateOfBirthLabel => 'Date of birth';

  @override
  String get authSelectDateOfBirth => 'Select date of birth';

  @override
  String get authOnboardingTitle => 'Learning setup';

  @override
  String get authSkipAction => 'Skip';

  @override
  String get authOnboardingGoalHeadline =>
      'What\'s your vocabulary learning goal?';

  @override
  String get authOnboardingTopicsHeadline =>
      'Which topics are you interested in?';

  @override
  String get authOnboardingGoalSubtitle =>
      'VocaNova will prioritize content based on this goal.';

  @override
  String get authOnboardingTopicsSubtitle =>
      'Choose at least one topic to get vocabulary suggestions that fit you.';

  @override
  String get authOnboardingFinishButton => 'Finish';

  @override
  String get authOnboardingContinueButton => 'Continue';

  @override
  String get authCatalogLoadError =>
      'Couldn\'t load the catalog. Please try again.';

  @override
  String get authRetryButton => 'Retry';

  @override
  String get authLearningProfileSaveFailed =>
      'Couldn\'t save your learning setup.';

  @override
  String get authLearningProfileSaveFailedRetry =>
      'Couldn\'t save your learning setup. Please try again.';

  @override
  String get authApiResponseInvalid => 'The API response data is invalid.';

  @override
  String get authGoogleTokenMissing =>
      'Google did not return a valid sign-in token.';

  @override
  String get dictBackTooltip => 'Back';

  @override
  String get dictSearchHint => 'Search for a word...';

  @override
  String get dictClearSearchTooltip => 'Clear search';

  @override
  String get dictAllLevelsLabel => 'All levels';

  @override
  String get dictAllTopicsLabel => 'All topics';

  @override
  String get dictSearchOfflineBanner =>
      'Offline — searching cached words and recent history only.';

  @override
  String get dictRecentSectionTitle => 'Recent';

  @override
  String get dictClearAction => 'Clear';

  @override
  String get dictBrowseByTopicTitle => 'Browse by topic';

  @override
  String get dictSeeAllAction => 'See all';

  @override
  String get dictRecentSearchesEmpty =>
      'Your recent searches will appear here.';

  @override
  String dictWordCountLabel(int count) {
    return '$count words';
  }

  @override
  String get dictNoMatchingCachedWords => 'No matching cached words';

  @override
  String get dictNoMatchingWords => 'No matching words found';

  @override
  String get dictReconnectHint => 'Reconnect to search the full dictionary.';

  @override
  String get dictAdjustFiltersHint =>
      'Try another spelling or adjust the filters.';

  @override
  String get dictTopicsTitle => 'Topics';

  @override
  String get dictSearchTopicsHint => 'Search topics...';

  @override
  String get dictNoPersonalTopicsMatch =>
      'No personal topics match your search.';

  @override
  String get dictNoSystemTopics => 'No system topics found.';

  @override
  String get dictSystemLibraryLabel => 'System library';

  @override
  String get dictMyTopicsLabel => 'My topics';

  @override
  String get dictPersonalModeNote =>
      'Only words you saved appear here, ready for practice.';

  @override
  String get dictSystemModeNote =>
      'Browse all words organized by the VocaNova team.';

  @override
  String dictPersonalWordCount(int count) {
    return '$count personal words';
  }

  @override
  String dictSystemWordCount(int count) {
    return '$count system words';
  }

  @override
  String get dictUnableToLoadTopics => 'Unable to load topics.';

  @override
  String get dictTryAgain => 'Try again';

  @override
  String get dictCategoryAll => 'All';

  @override
  String get dictCategoryEducation => 'Education';

  @override
  String get dictCategoryWork => 'Work';

  @override
  String get dictCategoryTravel => 'Travel';

  @override
  String get dictCategoryDailyLife => 'Daily life';

  @override
  String dictMyTopicTitle(String name) {
    return 'My $name';
  }

  @override
  String get dictTopicFallbackName => 'topic';

  @override
  String get dictTopicDetailFallbackTitle => 'Topic detail';

  @override
  String get dictUnableToLoadWordsRetry => 'Unable to load words. Try again';

  @override
  String get dictPracticeSavedWords => 'Practice my saved words';

  @override
  String get dictNoWordsInCategory => 'No words in this category.';

  @override
  String get dictRemovedFromTopic => 'Removed from your topic.';

  @override
  String get dictUnableToRemoveWord => 'Unable to remove this word.';

  @override
  String get dictAddToListTooltip => 'Add to list';

  @override
  String get dictRemoveFromTopicTooltip => 'Remove from my topic';

  @override
  String get dictStatMastered => 'Mastered';

  @override
  String get dictStatLearning => 'Learning';

  @override
  String get dictStatNew => 'New';

  @override
  String get dictStatAvgMastery => 'Avg mastery';

  @override
  String get dictWordNotFound => 'Word not found.';

  @override
  String get dictDefinitionLabel => 'Definition';

  @override
  String get dictVietnameseMeaningLabel => 'Vietnamese';

  @override
  String get dictUnableToPlayAudio => 'Unable to play audio.';

  @override
  String get dictWordCopied => 'Word copied to clipboard.';

  @override
  String get dictShareTooltip => 'Share';

  @override
  String get dictSavedTooltip => 'Saved';

  @override
  String get dictSaveWordTooltip => 'Save word';

  @override
  String get dictExampleLabel => 'Example';

  @override
  String get dictSynonymsLabel => 'Synonyms';

  @override
  String get dictAntonymsLabel => 'Antonyms';

  @override
  String get dictPracticeLabel => 'Practice';

  @override
  String get dictWordDetailOfflineBanner =>
      'Offline — showing saved word details.';

  @override
  String get dictAddToListSheetSubtitle =>
      'Choose a personal list or build a quiz-ready topic.';

  @override
  String get dictMyListsLabel => 'My lists';

  @override
  String get dictNewListLabel => 'New list';

  @override
  String get dictNoteOptionalLabel => 'Note (optional)';

  @override
  String get dictAddNoteHint => 'Add a note...';

  @override
  String get dictSaveToListLabel => 'Save to list';

  @override
  String get dictAddToTopicLabel => 'Add to topic';

  @override
  String get dictCreateListPrompt => 'Create a list to save this word.';

  @override
  String get dictNoSystemTopicAssignment =>
      'This word has no system topic assignment.';

  @override
  String get dictAlreadyInTopic => 'Already in your topic';

  @override
  String get dictUnableToLoadDestinations =>
      'Unable to load your save destinations.';

  @override
  String get dictListNameHint => 'List name';

  @override
  String get dictCancelLabel => 'Cancel';

  @override
  String get dictCreateLabel => 'Create';

  @override
  String get dictUnableToCreateList => 'Unable to create the list.';

  @override
  String dictAddedToDestination(String name) {
    return 'Added to $name.';
  }

  @override
  String get dictUnableToSaveWord => 'Unable to save this word.';

  @override
  String get dictNoSavedWordDataError => 'No saved word data available.';

  @override
  String get dictWordDetailLoadError => 'Unable to load word details.';

  @override
  String get dictSearchRefreshError =>
      'Unable to load new data. Showing saved words.';

  @override
  String get listsTitle => 'My Word Lists';

  @override
  String get listsCreateDialogTitle => 'Create list';

  @override
  String get listsRenameDialogTitle => 'Rename list';

  @override
  String get listsMyListsSection => 'My lists';

  @override
  String get listsPersonalTopicsSection => 'Personal topics';

  @override
  String get listsNameFieldHint => 'List name';

  @override
  String get listsCancel => 'Cancel';

  @override
  String get listsCreateAction => 'Create';

  @override
  String get listsSaveAction => 'Save';

  @override
  String get listsRenameAction => 'Rename';

  @override
  String get listsDeleteAction => 'Delete';

  @override
  String get listsDeleteConfirmTitle => 'Delete list?';

  @override
  String listsDeleteConfirmBody(String name) {
    return 'The list \"$name\" will be deleted.';
  }

  @override
  String get listsNameRequiredError => 'Please enter a list name.';

  @override
  String get listsNameMaxLengthError =>
      'List name can be at most 100 characters.';

  @override
  String listsWordCount(int count) {
    return '$count words';
  }

  @override
  String listsCreatedOnLabel(String date) {
    return 'Created on $date';
  }

  @override
  String get listsOfflineBanner => 'You\'re offline. Showing saved lists.';

  @override
  String get listsEmptyState =>
      'You don\'t have any word lists yet.\nTap + to create your first list.';

  @override
  String get listsDetailTitleFallback => 'List details';

  @override
  String get listsAddWordAction => 'Add word';

  @override
  String get listsDetailEmpty => 'This list doesn\'t have any words yet.';

  @override
  String get listsRemoveWordConfirmTitle => 'Remove word?';

  @override
  String listsRemoveWordConfirmBody(String word) {
    return 'Remove \"$word\" from the list?';
  }

  @override
  String get listsLoadTopicsError => 'Unable to load topics.';

  @override
  String get listsAddRandomDialogTitle => 'Add random words';

  @override
  String get listsByTopicOption => 'By topic';

  @override
  String get listsSynonymOption => 'Synonyms';

  @override
  String get listsAntonymOption => 'Antonyms';

  @override
  String get listsCountFieldLabel => 'Count (1-50)';

  @override
  String get listsAddAction => 'Add';

  @override
  String get listsAddRandomAction => 'Add random';

  @override
  String get listsStartQuizAction => 'Start quiz';

  @override
  String listsCorrectCount(int count) {
    return 'Correct: $count';
  }

  @override
  String listsWrongCount(int count) {
    return 'Wrong: $count';
  }

  @override
  String listsNoteLabel(String note) {
    return 'Note: $note';
  }

  @override
  String get listsDetailOfflineBanner =>
      'You\'re offline. Showing saved words.';

  @override
  String get listsSearchWordHint => 'Search for an English word';

  @override
  String get listsLoadListsError => 'Unable to load word lists.';

  @override
  String get listsCreateError =>
      'Unable to create list. The name may already exist.';

  @override
  String get listsRenameError => 'Unable to rename list.';

  @override
  String get listsDeleteError => 'Unable to delete list.';

  @override
  String get listsOfflineMutateError =>
      'An internet connection is required to change lists.';

  @override
  String get listsLoadWordsError => 'Unable to load words in this list.';

  @override
  String get listsLoadMoreWordsError => 'Unable to load more words.';

  @override
  String get listsAddWordError => 'Unable to add word to the list.';

  @override
  String get listsAddRandomError => 'Unable to add random words.';

  @override
  String get listsRemoveWordError => 'Unable to remove word from the list.';

  @override
  String get progressOverviewTitle => 'Learning Progress';

  @override
  String get progressChartsTooltip => 'Detailed charts';

  @override
  String progressStreakDaysLabel(int days) {
    return '$days days in a row';
  }

  @override
  String progressLongestStreakLabel(int days) {
    return 'Record: $days days';
  }

  @override
  String get progressAccuracy7DaysLabel => '7-day accuracy';

  @override
  String progressCorrectAnswersLabel(int correct, int total) {
    return '$correct/$total correct';
  }

  @override
  String get progressWordsInProgressLabel => 'Words in progress';

  @override
  String get progressMasteredWordsLabel => 'Mastered words';

  @override
  String get progressSessionsThisMonthLabel => 'Quizzes this month';

  @override
  String get progressOfflineBanner =>
      'You\'re offline. Showing saved progress data.';

  @override
  String get progressNoDataMessage => 'No progress data yet.';

  @override
  String get progressRetry => 'Retry';

  @override
  String get progressChartsTitle => 'Progress charts';

  @override
  String get progressGranularityDaily => 'Daily';

  @override
  String get progressGranularityWeekly => 'Weekly';

  @override
  String get progressGranularityMonthly => 'Monthly';

  @override
  String get progressSessionsCountLabel => 'Number of sessions';

  @override
  String get progressMasteryLevelLabel => 'Mastery level';

  @override
  String get progressTop10WeakestWordsLabel => '10 weakest words';

  @override
  String get progressNoWeakestWords => 'No weak words to review yet.';

  @override
  String progressMasteryLevelShort(int level) {
    return 'Lv.$level';
  }

  @override
  String progressWordStatsLabel(int correct, int wrong) {
    return 'Correct $correct · Wrong $wrong';
  }

  @override
  String get progressNoCachedDataError => 'No saved progress data.';

  @override
  String get progressLoadOverviewError => 'Couldn\'t load progress overview.';

  @override
  String get progressLoadChartsError => 'Couldn\'t load progress charts.';

  @override
  String get progressChangeGranularityError =>
      'Couldn\'t change the chart\'s time range.';

  @override
  String get homeWelcomeBack => 'Welcome back';

  @override
  String homeGreetingName(String name) {
    return 'Hi, $name';
  }

  @override
  String get homeGreetingMorning => 'Good morning';

  @override
  String get homeGreetingAfternoon => 'Good afternoon';

  @override
  String get homeGreetingEvening => 'Good evening';

  @override
  String get homeSeeAll => 'See all';

  @override
  String get homeSearchHint => 'Search a word...';

  @override
  String get homeDailyGoalLabel => 'DAILY GOAL';

  @override
  String homeGoalProgress(int mastered, int total) {
    return '$mastered / $total words';
  }

  @override
  String get homeMasteredSoFar => 'mastered so far';

  @override
  String homeStreakActive(int days) {
    return 'Keep your $days-day streak';
  }

  @override
  String get homeStreakInactive => 'Start a streak today';

  @override
  String get homeWordOfTheDayLabel => 'WORD OF THE DAY';

  @override
  String get homeLearnThisWord => 'Learn this word';

  @override
  String get homeStatWords => 'Words';

  @override
  String get homeStatAccuracy => 'Accuracy';

  @override
  String get homeStatMastered => 'Mastered';

  @override
  String get homeContinueLabel => 'CONTINUE';

  @override
  String homeWordCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count words',
      one: '$count word',
    );
    return '$_temp0';
  }

  @override
  String get homeQuickActionsTitle => 'Quick actions';

  @override
  String get homeActionQuiz => 'Quiz';

  @override
  String get homeActionReview => 'Review';

  @override
  String get homeActionTopics => 'Topics';

  @override
  String get homeTopicsForYouTitle => 'Topics for you';

  @override
  String homeTopicWordCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count words',
      one: '$count word',
    );
    return '$_temp0';
  }

  @override
  String homeWordsToReviewLabel(String count) {
    return '$count words to review';
  }

  @override
  String get homeTapToReviewMistakes => 'Tap to review your mistakes';

  @override
  String get homeMyListsTitle => 'My lists';

  @override
  String get homeCreateFirstList => 'Create your first list to start learning';

  @override
  String get homeWeekdayMonShort => 'M';

  @override
  String get homeWeekdayTueShort => 'T';

  @override
  String get homeWeekdayWedShort => 'W';

  @override
  String get homeWeekdayThuShort => 'T';

  @override
  String get homeWeekdayFriShort => 'F';

  @override
  String get homeWeekdaySatShort => 'S';

  @override
  String get homeWeekdaySunShort => 'S';

  @override
  String get notifTitle => 'Notifications';

  @override
  String get notifMarkAllRead => 'Mark all read';

  @override
  String get notifEmptyMessage => 'No notifications yet.';

  @override
  String get notifRetry => 'Retry';

  @override
  String get notifJustNow => 'Just now';

  @override
  String notifMinutesAgo(int count) {
    return '$count min ago';
  }

  @override
  String notifHoursAgo(int count) {
    return '$count hr ago';
  }

  @override
  String notifDaysAgo(int count) {
    return '$count days ago';
  }

  @override
  String get notifLoadError => 'Couldn\'t load notifications.';

  @override
  String get quizConfigTitle => 'Quiz setup';

  @override
  String get quizConfigScopeSection => 'Word scope';

  @override
  String get quizConfigScopeAll => 'All';

  @override
  String get quizConfigScopeFromDate => 'From date';

  @override
  String get quizConfigScopeToDate => 'To date';

  @override
  String get quizConfigScopeDateRange => 'Date range';

  @override
  String get quizConfigScopeWrongWords => 'Recently wrong words';

  @override
  String get quizConfigDateFrom => 'Start date';

  @override
  String get quizConfigDateTo => 'End date';

  @override
  String get quizConfigSourceSection => 'Quiz source';

  @override
  String get quizConfigModeSection => 'Mode';

  @override
  String get quizConfigQuestionTypeSection => 'Question type';

  @override
  String get quizConfigAnswerMethodSection => 'Answer method';

  @override
  String get quizConfigWordOrderSection => 'Order';

  @override
  String get quizConfigQuestionLimitSection => 'Question count';

  @override
  String get quizConfigNeedConnection => 'Internet connection required';

  @override
  String get quizConfigStartButton => 'Start';

  @override
  String get quizConfigQuestionTypeWordToMeaning => 'Word → meaning';

  @override
  String get quizConfigQuestionTypeMeaningToWord => 'Meaning → word';

  @override
  String get quizConfigQuestionTypeDescToWord => 'Description → word';

  @override
  String get quizAnswerMultipleChoice => 'Multiple choice';

  @override
  String get quizAnswerTyping => 'Typing';

  @override
  String get quizAnswerAiTyping => 'AI typing';

  @override
  String get quizConfigOrderRandom => 'Random';

  @override
  String get quizConfigOrderNewest => 'Newest';

  @override
  String get quizConfigOrderOldest => 'Oldest';

  @override
  String get quizConfigOrderByDifficulty => 'By difficulty';

  @override
  String get quizModeStandard => 'Standard';

  @override
  String get quizModeTimed => 'Timed';

  @override
  String get quizModeChallenge => 'Challenge';

  @override
  String get quizModeElimination => 'Elimination';

  @override
  String get quizConfigModeStandardSubtitle => 'At your own pace';

  @override
  String get quizConfigModeTimedSubtitle => 'Race against the clock';

  @override
  String get quizConfigModeChallengeSubtitle => 'Lives and correct streaks';

  @override
  String get quizConfigModeEliminationSubtitle => 'One mistake ends it';

  @override
  String get quizConfigTimeLimitLabel => 'Time limit (seconds)';

  @override
  String get quizConfigLivesLabel => 'Lives';

  @override
  String get quizConfigLimitAll => 'All';

  @override
  String get quizConfigLimitCustom => 'Custom';

  @override
  String get quizConfigCustomLimitLabel => 'Desired question count';

  @override
  String quizConfigCustomLimitHelper(int wordCount) {
    return 'Source has $wordCount words · a higher value will be capped at $wordCount';
  }

  @override
  String get quizConfigCustomLimitHelperEmpty =>
      'Enter the desired question count';

  @override
  String get quizConfigSourceMyList => 'My lists';

  @override
  String get quizConfigSourcePersonalTopic => 'Personal topics';

  @override
  String get quizConfigSourceHintList => 'Choose one of your word lists.';

  @override
  String get quizConfigSourceHintTopic =>
      'Choose a topic containing your saved words.';

  @override
  String get quizConfigSourceNoWords => 'No words to quiz yet';

  @override
  String quizConfigSourceWordCount(int count) {
    return '$count words';
  }

  @override
  String get quizConfigEmptyTopicHint =>
      'Save some words to a personal topic before taking a quiz.';

  @override
  String get quizConfigEmptyListHint =>
      'Create a list and add words before taking a quiz.';

  @override
  String get quizConfigSummaryMode => 'Mode';

  @override
  String get quizConfigSummaryAnswer => 'Answer';

  @override
  String get quizConfigSummaryCount => 'Questions';

  @override
  String get quizConfigLoadSourcesError => 'Could not load quiz sources.';

  @override
  String get quizConfigValidateNoSource =>
      'Please choose a list or personal topic to quiz on.';

  @override
  String get quizConfigValidateNoQuestionCount =>
      'Please enter the number of questions.';

  @override
  String get quizConfigValidateQuestionCountPositive =>
      'The number of questions must be greater than 0.';

  @override
  String get quizConfigValidateDateFromRequired =>
      'Please choose a start date.';

  @override
  String get quizConfigValidateDateToRequired => 'Please choose an end date.';

  @override
  String get quizConfigValidateDateOrder =>
      'The start date must be before or equal to the end date.';

  @override
  String get quizConfigValidateTimedPositive =>
      'Timed mode needs a time limit greater than 0.';

  @override
  String get quizConfigValidateEliminationPositive =>
      'Elimination mode needs more than 0 lives.';

  @override
  String get quizConfigCreateSessionError =>
      'Could not create the quiz. Please check the word count.';

  @override
  String get quizSessionTitle => 'Quiz';

  @override
  String get quizSessionAbandonTooltip => 'Quit quiz';

  @override
  String quizSessionQuestionNumber(int number) {
    return 'Question $number';
  }

  @override
  String get quizSessionFinishing => 'Finishing...';

  @override
  String get quizSessionViewResult => 'View result';

  @override
  String get quizSessionNext => 'Next';

  @override
  String get quizSessionAbandonDialogTitle => 'Quit the quiz?';

  @override
  String get quizSessionAbandonDialogContent =>
      'Your current progress will be ended and saved.';

  @override
  String get quizSessionAbandonCancel => 'Keep going';

  @override
  String get quizSessionAbandonConfirm => 'Quit';

  @override
  String quizSessionProgressLabel(int current, int total) {
    return 'Question $current/$total';
  }

  @override
  String get quizSessionUnavailableMessage =>
      'Could not restore the running quiz. Please start a new one.';

  @override
  String get quizSessionCreateNew => 'Create a quiz';

  @override
  String get quizTypingLabelAi => 'Type your answer';

  @override
  String get quizTypingLabelDefault => 'Type the answer';

  @override
  String get quizTypingHelperAi =>
      'AI will grade how accurate your meaning is.';

  @override
  String get quizTypingHelperDefault =>
      'Case and trailing punctuation don\'t matter.';

  @override
  String get quizTypingAiEvaluating => 'AI is grading...';

  @override
  String get quizTypingSubmit => 'Submit answer';

  @override
  String get quizTypingEmptyAnswer => 'Please enter an answer.';

  @override
  String get quizTypingCorrect => 'Correct';

  @override
  String get quizTypingIncorrect => 'Not quite right';

  @override
  String quizTypingExpectedAnswer(String answer) {
    return 'Answer: $answer';
  }

  @override
  String quizTypingAiScore(int score) {
    return 'AI score: $score%';
  }

  @override
  String quizTypingAiSuggestion(String suggestion) {
    return 'Suggestion: $suggestion';
  }

  @override
  String get quizSessionSubmitError =>
      'Could not submit the answer. Please try again.';

  @override
  String get quizSessionFinishError =>
      'Could not finish the quiz. Please try again.';

  @override
  String get quizResultHeadlineGreat => 'Excellent!';

  @override
  String get quizResultHeadlineGood => 'Well done!';

  @override
  String get quizResultHeadlineTryAgain => 'Keep practicing!';

  @override
  String quizResultSummary(int correct, int total, String duration) {
    return '$correct/$total correct · $duration';
  }

  @override
  String get quizResultAccuracyLabel => 'accuracy';

  @override
  String get quizResultCorrectLabel => 'Correct';

  @override
  String get quizResultWrongLabel => 'Wrong';

  @override
  String get quizResultBestStreakLabel => 'Best streak';

  @override
  String get quizResultListTitle => 'RESULTS';

  @override
  String get quizResultYourAnswerLabel => 'You answered';

  @override
  String get quizResultNoAnswer => 'No answer';

  @override
  String get quizResultAnswerLabel => 'Answer';

  @override
  String get quizResultReviewWrongButton => 'Review wrong words';

  @override
  String get quizResultRetryButton => 'Retry';

  @override
  String get quizResultDoneButton => 'Done';

  @override
  String get quizResultLoadError => 'Could not load the quiz result.';

  @override
  String get quizResultRetryLoadButton => 'Try again';

  @override
  String get quizResultInvalidSession => 'Invalid quiz session code.';

  @override
  String get quizWrongWordsTitle => 'Wrong words';

  @override
  String get quizWrongWordsRetryButton => 'Retest';

  @override
  String get quizWrongWordsEmpty =>
      'You don\'t have any words in your wrong list yet.';

  @override
  String get quizWrongWordsNoMeaning => 'No meaning yet';

  @override
  String quizWrongWordsStats(int correct, int wrong) {
    return 'Correct: $correct · Wrong: $wrong';
  }

  @override
  String quizWrongWordsMasteryLevel(int level) {
    return 'Lv.$level';
  }

  @override
  String get quizWrongWordsLoadError => 'Could not load the wrong words list.';

  @override
  String get quizWrongWordsLoadMoreError => 'Could not load more wrong words.';

  @override
  String get quizWrongWordsRemoveError =>
      'Could not remove the word from the wrong list.';

  @override
  String get settingsSectionAppearance => 'APPEARANCE';

  @override
  String get settingsDarkMode => 'Dark mode';

  @override
  String get settingsDarkModeSubtitle => 'Switch to dark theme';

  @override
  String get settingsFollowSystemTheme => 'Follow system theme';

  @override
  String get settingsSectionLanguage => 'LANGUAGE';

  @override
  String get settingsAppLanguage => 'App language';

  @override
  String get settingsSectionNotifications => 'NOTIFICATIONS';

  @override
  String get settingsDailyReminder => 'Daily reminder';

  @override
  String get settingsDailyReminderSubtitle => 'Remind me to study each day';

  @override
  String get settingsStreakAlert => 'Streak alert';

  @override
  String get settingsStreakAlertSubtitle => 'Warn before streak breaks';

  @override
  String get settingsReviewDue => 'Review due';

  @override
  String get settingsReviewDueSubtitle => 'Words ready for spaced review';

  @override
  String get settingsSectionAudio => 'AUDIO';

  @override
  String get settingsAutoPlayPronunciation => 'Auto-play pronunciation';

  @override
  String get settingsAutoPlayPronunciationSubtitle =>
      'Play when viewing a word';

  @override
  String get settingsSoundEffects => 'Sound effects';

  @override
  String get settingsSoundEffectsSubtitle => 'Quiz sounds and feedback';

  @override
  String get settingsSectionAccount => 'ACCOUNT';

  @override
  String get settingsPrivacyPolicy => 'Privacy policy';

  @override
  String get settingsDeleteAccount => 'Delete account';

  @override
  String settingsVersionLabel(String version) {
    return 'VocaNova v$version';
  }

  @override
  String get settingsPrivacyPolicyBody =>
      'VocaNova stores only the account and learning data needed to provide vocabulary practice, progress tracking, and synchronization. Your credentials are protected and are never displayed in the app.';

  @override
  String get settingsBackToProfile => 'Profile';

  @override
  String get settingsTitle => 'Settings';

  @override
  String get settingsDeleteAccountDialogTitle => 'Delete account?';

  @override
  String get settingsDeleteAccountDialogBody =>
      'This action permanently removes your profile and learning data. It cannot be undone.';

  @override
  String get settingsCancel => 'Cancel';

  @override
  String get settingsContinue => 'Continue';

  @override
  String get settingsDeleteAccountFailed =>
      'Unable to delete the account. Please try again.';

  @override
  String get settingsDone => 'Done';

  @override
  String get profileSectionLearning => 'LEARNING';

  @override
  String get profileMyVocabulary => 'My vocabulary';

  @override
  String get profileMyVocabularySubtitle => '248 words collected';

  @override
  String get profileStatistics => 'Statistics';

  @override
  String get profileStatisticsSubtitle => 'Progress & analytics';

  @override
  String get profileTestHistory => 'Test history';

  @override
  String get profileTestHistorySubtitle => 'Past practice sessions';

  @override
  String get profileLearningGoals => 'Learning goals';

  @override
  String get profileLearningGoalsSubtitle => 'B2 → C1 target';

  @override
  String get profileSectionAccount => 'ACCOUNT';

  @override
  String get profilePersonalInformation => 'Personal information';

  @override
  String get profilePersonalInformationSubtitle => 'Name, avatar, phone';

  @override
  String get profileNotifications => 'Notifications';

  @override
  String get profileDailyRemindersOn => 'Daily reminders on';

  @override
  String get profileDailyRemindersOff => 'Daily reminders off';

  @override
  String get profileLanguage => 'Language';

  @override
  String get profileLanguageEnglish => 'English';

  @override
  String get profileLanguageVietnamese => 'Vietnamese';

  @override
  String get profileTheme => 'Theme';

  @override
  String get profileThemeDark => 'Dark mode';

  @override
  String get profileThemeLight => 'Light mode';

  @override
  String get profileThemeSystem => 'System default';

  @override
  String get profileSectionApp => 'APP';

  @override
  String get profileSettingsMenuTitle => 'Settings';

  @override
  String get profileSettingsMenuSubtitle => 'Audio, storage, sync';

  @override
  String get profilePrivacyData => 'Privacy & data';

  @override
  String get profilePrivacyDataSubtitle => 'Manage your data';

  @override
  String get profilePrivacyDataBody =>
      'VocaNova stores your profile and learning progress so your vocabulary can stay synchronized across sessions.';

  @override
  String get profileHelpFeedback => 'Help & feedback';

  @override
  String get profileHelpFeedbackSubtitle => 'FAQs and support';

  @override
  String get profileHelpFeedbackBody =>
      'Need a hand? Share the issue, the screen you were using, and the steps that caused it with the VocaNova support team.';

  @override
  String get profileSignOut => 'Sign out';

  @override
  String get profileVersionLabel => 'VocaNova v1.0.0 · SEP490_19';

  @override
  String get profileUploadAvatarFailed => 'Unable to upload avatar.';

  @override
  String get profileUpdateSuccess => 'Profile updated successfully.';

  @override
  String get profileUpdateFailed => 'Unable to update profile.';

  @override
  String get profilePasswordChangeSuccess => 'Password changed successfully.';

  @override
  String get profilePasswordChangeFailed => 'Unable to change password.';

  @override
  String get profileDone => 'Done';

  @override
  String get profileSignOutConfirmTitle => 'Sign out?';

  @override
  String get profileSignOutConfirmBody =>
      'You will need to sign in again to keep learning.';

  @override
  String get profileCancel => 'Cancel';

  @override
  String get profilePhoneNotLinked => 'Phone not linked';

  @override
  String get profileLevelB2 => 'B2 level';

  @override
  String profileStreakLabel(int days) {
    return '$days-day streak';
  }

  @override
  String get profileEditAction => 'Edit';

  @override
  String get profileStatWords => 'Words';

  @override
  String get profileStatAccuracy => 'Accuracy';

  @override
  String get profileStatStreak => 'Streak';

  @override
  String get profileStatBadges => 'Badges';

  @override
  String get profileEditSubtitle => 'Update your profile details.';

  @override
  String get profileFieldPicture => 'Profile picture';

  @override
  String get profileChooseAvatar => 'Choose an avatar';

  @override
  String get profileFieldFullName => 'Full name';

  @override
  String get profileNameHint => 'Nguyen Van An';

  @override
  String get profileNameTooShort => 'Name must contain at least 2 characters';

  @override
  String get profileFieldPhoneNumber => 'Phone number';

  @override
  String get profilePhoneNotLinkedShort => 'Not linked';

  @override
  String get profileChangePassword => 'Change password';

  @override
  String get profileSaveChanges => 'Save changes';

  @override
  String get profileAvatarOpening => 'Opening...';

  @override
  String get profileChooseFromDevice => 'Choose from device';

  @override
  String get profileAvatarHint => 'JPG, PNG or WebP · Max 5MB';

  @override
  String get profileAvatarTooLarge => 'Avatar must be 5MB or smaller.';

  @override
  String get profilePhotoLibraryError => 'Unable to open the photo library.';

  @override
  String get profileChangePasswordSubtitle =>
      'Use at least 8 characters with upper, lower and a number.';

  @override
  String get profileFieldCurrentPassword => 'Current password';

  @override
  String get profileCurrentPasswordHint => 'Enter current password';

  @override
  String get profileFieldNewPassword => 'New password';

  @override
  String get profileNewPasswordHint => 'At least 8 characters';

  @override
  String get profileFieldConfirmPassword => 'Confirm new password';

  @override
  String get profileConfirmPasswordHint => 'Repeat your password';

  @override
  String get profileUpdatePassword => 'Update password';

  @override
  String get profileClose => 'Close';

  @override
  String get profileHidePassword => 'Hide password';

  @override
  String get profileShowPassword => 'Show password';

  @override
  String get profileTryAgain => 'Try again';
}
