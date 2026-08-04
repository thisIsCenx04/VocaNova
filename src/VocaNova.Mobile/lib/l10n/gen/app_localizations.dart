import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_vi.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'gen/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('vi'),
  ];

  /// Application title
  ///
  /// In en, this message translates to:
  /// **'VocaNova'**
  String get appTitle;

  /// No description provided for @navHome.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get navHome;

  /// No description provided for @navSearch.
  ///
  /// In en, this message translates to:
  /// **'Search'**
  String get navSearch;

  /// No description provided for @navLists.
  ///
  /// In en, this message translates to:
  /// **'Lists'**
  String get navLists;

  /// No description provided for @navPractice.
  ///
  /// In en, this message translates to:
  /// **'Practice'**
  String get navPractice;

  /// No description provided for @navProfile.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get navProfile;

  /// No description provided for @commonOfflineBanner.
  ///
  /// In en, this message translates to:
  /// **'You\'re offline'**
  String get commonOfflineBanner;

  /// No description provided for @authBackButton.
  ///
  /// In en, this message translates to:
  /// **'Back'**
  String get authBackButton;

  /// No description provided for @authOrDivider.
  ///
  /// In en, this message translates to:
  /// **'or'**
  String get authOrDivider;

  /// No description provided for @authContinueWithGoogle.
  ///
  /// In en, this message translates to:
  /// **'Continue with Google'**
  String get authContinueWithGoogle;

  /// No description provided for @authGenericError.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get authGenericError;

  /// No description provided for @authPhoneRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your phone number.'**
  String get authPhoneRequired;

  /// No description provided for @authPhoneInvalid.
  ///
  /// In en, this message translates to:
  /// **'Invalid Vietnamese phone number.'**
  String get authPhoneInvalid;

  /// No description provided for @authPasswordRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your password.'**
  String get authPasswordRequired;

  /// No description provided for @authPasswordTooShort.
  ///
  /// In en, this message translates to:
  /// **'Password must be at least 8 characters.'**
  String get authPasswordTooShort;

  /// No description provided for @authPasswordComplexity.
  ///
  /// In en, this message translates to:
  /// **'Password needs an uppercase letter, a lowercase letter, and a digit.'**
  String get authPasswordComplexity;

  /// No description provided for @authDisplayNameRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter a display name.'**
  String get authDisplayNameRequired;

  /// No description provided for @authDisplayNameTooShort.
  ///
  /// In en, this message translates to:
  /// **'Display name must be at least 2 characters.'**
  String get authDisplayNameTooShort;

  /// No description provided for @authDisplayNameTooLong.
  ///
  /// In en, this message translates to:
  /// **'Display name must not exceed 150 characters.'**
  String get authDisplayNameTooLong;

  /// No description provided for @authConfirmPasswordRequired.
  ///
  /// In en, this message translates to:
  /// **'Please confirm your password.'**
  String get authConfirmPasswordRequired;

  /// No description provided for @authConfirmPasswordMismatch.
  ///
  /// In en, this message translates to:
  /// **'Passwords do not match.'**
  String get authConfirmPasswordMismatch;

  /// No description provided for @authForgotTitleReset.
  ///
  /// In en, this message translates to:
  /// **'Reset password'**
  String get authForgotTitleReset;

  /// No description provided for @authForgotTitleVerify.
  ///
  /// In en, this message translates to:
  /// **'Verify your code'**
  String get authForgotTitleVerify;

  /// No description provided for @authForgotTitleCreate.
  ///
  /// In en, this message translates to:
  /// **'Create password'**
  String get authForgotTitleCreate;

  /// No description provided for @authForgotSubtitlePhone.
  ///
  /// In en, this message translates to:
  /// **'Enter your phone number and we\'ll send a code to reset your password.'**
  String get authForgotSubtitlePhone;

  /// No description provided for @authForgotSubtitleOtp.
  ///
  /// In en, this message translates to:
  /// **'Enter the 6-digit code sent to {phone}.'**
  String authForgotSubtitleOtp(String phone);

  /// No description provided for @authForgotSubtitlePassword.
  ///
  /// In en, this message translates to:
  /// **'Create a new password for your account.'**
  String get authForgotSubtitlePassword;

  /// No description provided for @authStepProgress.
  ///
  /// In en, this message translates to:
  /// **'Step {current}/{total}'**
  String authStepProgress(int current, int total);

  /// No description provided for @authEmailOrPhoneLabel.
  ///
  /// In en, this message translates to:
  /// **'Email or phone number'**
  String get authEmailOrPhoneLabel;

  /// No description provided for @authSendResetCodeButton.
  ///
  /// In en, this message translates to:
  /// **'Send reset code'**
  String get authSendResetCodeButton;

  /// No description provided for @authOtpMaxAttemptsReached.
  ///
  /// In en, this message translates to:
  /// **'You\'ve entered the wrong OTP more than 5 times. Please resend the code.'**
  String get authOtpMaxAttemptsReached;

  /// No description provided for @authOtpVerifiedOnSave.
  ///
  /// In en, this message translates to:
  /// **'The OTP code will be checked when you save the new password.'**
  String get authOtpVerifiedOnSave;

  /// No description provided for @authResendCode.
  ///
  /// In en, this message translates to:
  /// **'Resend code'**
  String get authResendCode;

  /// No description provided for @authResendInSeconds.
  ///
  /// In en, this message translates to:
  /// **'Resend in {seconds}s'**
  String authResendInSeconds(int seconds);

  /// No description provided for @authChangePhoneNumber.
  ///
  /// In en, this message translates to:
  /// **'Change phone number'**
  String get authChangePhoneNumber;

  /// No description provided for @authNewPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'New password'**
  String get authNewPasswordLabel;

  /// No description provided for @authConfirmNewPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'Confirm new password'**
  String get authConfirmNewPasswordLabel;

  /// No description provided for @authSaveNewPasswordButton.
  ///
  /// In en, this message translates to:
  /// **'Save new password'**
  String get authSaveNewPasswordButton;

  /// No description provided for @authEnterOtpAgain.
  ///
  /// In en, this message translates to:
  /// **'Enter OTP again'**
  String get authEnterOtpAgain;

  /// No description provided for @authShowPassword.
  ///
  /// In en, this message translates to:
  /// **'Show password'**
  String get authShowPassword;

  /// No description provided for @authHidePassword.
  ///
  /// In en, this message translates to:
  /// **'Hide password'**
  String get authHidePassword;

  /// No description provided for @authOtpResentMessage.
  ///
  /// In en, this message translates to:
  /// **'OTP code resent.'**
  String get authOtpResentMessage;

  /// No description provided for @authPasswordChangedMessage.
  ///
  /// In en, this message translates to:
  /// **'Password changed.'**
  String get authPasswordChangedMessage;

  /// No description provided for @authSignInTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get authSignInTitle;

  /// No description provided for @authSignInSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Welcome to VocaNova'**
  String get authSignInSubtitle;

  /// No description provided for @authPhoneNumberLabel.
  ///
  /// In en, this message translates to:
  /// **'Phone number'**
  String get authPhoneNumberLabel;

  /// No description provided for @authPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get authPasswordLabel;

  /// No description provided for @authForgotPasswordLink.
  ///
  /// In en, this message translates to:
  /// **'Forgot password?'**
  String get authForgotPasswordLink;

  /// No description provided for @authNewHerePrefix.
  ///
  /// In en, this message translates to:
  /// **'New here? '**
  String get authNewHerePrefix;

  /// No description provided for @authCreateAccountTitle.
  ///
  /// In en, this message translates to:
  /// **'Create account'**
  String get authCreateAccountTitle;

  /// No description provided for @authVerifyPhoneTitle.
  ///
  /// In en, this message translates to:
  /// **'Verify your phone number'**
  String get authVerifyPhoneTitle;

  /// No description provided for @authOtpSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Enter the 6-digit code sent to\n{phone}'**
  String authOtpSubtitle(String phone);

  /// No description provided for @authVerifyButton.
  ///
  /// In en, this message translates to:
  /// **'Verify'**
  String get authVerifyButton;

  /// No description provided for @authAttemptsRemaining.
  ///
  /// In en, this message translates to:
  /// **'You have {count} attempts remaining.'**
  String authAttemptsRemaining(int count);

  /// No description provided for @authDidntReceiveCodePrefix.
  ///
  /// In en, this message translates to:
  /// **'Didn\'t receive it? '**
  String get authDidntReceiveCodePrefix;

  /// No description provided for @authOtpVerifiedSuccessMessage.
  ///
  /// In en, this message translates to:
  /// **'OTP verified successfully.'**
  String get authOtpVerifiedSuccessMessage;

  /// No description provided for @authRegistrationDataMissing.
  ///
  /// In en, this message translates to:
  /// **'Missing registration data.'**
  String get authRegistrationDataMissing;

  /// No description provided for @authStartLearningSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Start learning today'**
  String get authStartLearningSubtitle;

  /// No description provided for @authFullNameLabel.
  ///
  /// In en, this message translates to:
  /// **'Full name'**
  String get authFullNameLabel;

  /// No description provided for @authPasswordHintMinChars.
  ///
  /// In en, this message translates to:
  /// **'At least 8 characters'**
  String get authPasswordHintMinChars;

  /// No description provided for @authConfirmPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'Confirm password'**
  String get authConfirmPasswordLabel;

  /// No description provided for @authRepeatPasswordHint.
  ///
  /// In en, this message translates to:
  /// **'Repeat your password'**
  String get authRepeatPasswordHint;

  /// No description provided for @authAlreadyHaveAccountPrefix.
  ///
  /// In en, this message translates to:
  /// **'Already have an account? '**
  String get authAlreadyHaveAccountPrefix;

  /// No description provided for @authLearningProfileSectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Personalize learning suggestions (optional)'**
  String get authLearningProfileSectionTitle;

  /// No description provided for @authLearningProfileSectionSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Help VocaNova suggest topics and vocabulary that fit you from day one.'**
  String get authLearningProfileSectionSubtitle;

  /// No description provided for @authRegionLabel.
  ///
  /// In en, this message translates to:
  /// **'Region'**
  String get authRegionLabel;

  /// No description provided for @authOccupationLabel.
  ///
  /// In en, this message translates to:
  /// **'Occupation'**
  String get authOccupationLabel;

  /// No description provided for @authEducationLevelLabel.
  ///
  /// In en, this message translates to:
  /// **'Education level'**
  String get authEducationLevelLabel;

  /// No description provided for @authDateOfBirthLabel.
  ///
  /// In en, this message translates to:
  /// **'Date of birth'**
  String get authDateOfBirthLabel;

  /// No description provided for @authSelectDateOfBirth.
  ///
  /// In en, this message translates to:
  /// **'Select date of birth'**
  String get authSelectDateOfBirth;

  /// No description provided for @authOnboardingTitle.
  ///
  /// In en, this message translates to:
  /// **'Learning setup'**
  String get authOnboardingTitle;

  /// No description provided for @authSkipAction.
  ///
  /// In en, this message translates to:
  /// **'Skip'**
  String get authSkipAction;

  /// No description provided for @authOnboardingGoalHeadline.
  ///
  /// In en, this message translates to:
  /// **'What\'s your vocabulary learning goal?'**
  String get authOnboardingGoalHeadline;

  /// No description provided for @authOnboardingTopicsHeadline.
  ///
  /// In en, this message translates to:
  /// **'Which topics are you interested in?'**
  String get authOnboardingTopicsHeadline;

  /// No description provided for @authOnboardingGoalSubtitle.
  ///
  /// In en, this message translates to:
  /// **'VocaNova will prioritize content based on this goal.'**
  String get authOnboardingGoalSubtitle;

  /// No description provided for @authOnboardingTopicsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Choose at least one topic to get vocabulary suggestions that fit you.'**
  String get authOnboardingTopicsSubtitle;

  /// No description provided for @authOnboardingFinishButton.
  ///
  /// In en, this message translates to:
  /// **'Finish'**
  String get authOnboardingFinishButton;

  /// No description provided for @authOnboardingContinueButton.
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get authOnboardingContinueButton;

  /// No description provided for @authCatalogLoadError.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t load the catalog. Please try again.'**
  String get authCatalogLoadError;

  /// No description provided for @authRetryButton.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get authRetryButton;

  /// No description provided for @authLearningProfileSaveFailed.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t save your learning setup.'**
  String get authLearningProfileSaveFailed;

  /// No description provided for @authLearningProfileSaveFailedRetry.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t save your learning setup. Please try again.'**
  String get authLearningProfileSaveFailedRetry;

  /// No description provided for @authApiResponseInvalid.
  ///
  /// In en, this message translates to:
  /// **'The API response data is invalid.'**
  String get authApiResponseInvalid;

  /// No description provided for @authGoogleTokenMissing.
  ///
  /// In en, this message translates to:
  /// **'Google did not return a valid sign-in token.'**
  String get authGoogleTokenMissing;

  /// No description provided for @authGoogleClientIdMissing.
  ///
  /// In en, this message translates to:
  /// **'Google Sign-In is not configured. Run Flutter with --dart-define=GOOGLE_SERVER_CLIENT_ID=YOUR_WEB_CLIENT_ID.'**
  String get authGoogleClientIdMissing;

  /// No description provided for @authGoogleClientConfigurationError.
  ///
  /// In en, this message translates to:
  /// **'Google Sign-In configuration is invalid. Check the Android package name, signing SHA-1, and Web client ID.'**
  String get authGoogleClientConfigurationError;

  /// No description provided for @authGoogleProviderConfigurationError.
  ///
  /// In en, this message translates to:
  /// **'Google Play services or the Google provider is unavailable or incorrectly configured on this device.'**
  String get authGoogleProviderConfigurationError;

  /// No description provided for @authGoogleUiUnavailable.
  ///
  /// In en, this message translates to:
  /// **'Google could not open the account selection screen. Please reopen the app and try again.'**
  String get authGoogleUiUnavailable;

  /// No description provided for @authGoogleInterrupted.
  ///
  /// In en, this message translates to:
  /// **'Google Sign-In was interrupted. Please try again.'**
  String get authGoogleInterrupted;

  /// No description provided for @authGoogleCanceled.
  ///
  /// In en, this message translates to:
  /// **'Google Sign-In was canceled. If you selected an account before seeing this message, check the Android package name, SHA-1, and Web client ID.'**
  String get authGoogleCanceled;

  /// No description provided for @authGoogleUnknownError.
  ///
  /// In en, this message translates to:
  /// **'Google Sign-In failed: {details}'**
  String authGoogleUnknownError(String details);

  /// No description provided for @dictBackTooltip.
  ///
  /// In en, this message translates to:
  /// **'Back'**
  String get dictBackTooltip;

  /// No description provided for @dictSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Search for a word...'**
  String get dictSearchHint;

  /// No description provided for @dictClearSearchTooltip.
  ///
  /// In en, this message translates to:
  /// **'Clear search'**
  String get dictClearSearchTooltip;

  /// No description provided for @dictAllLevelsLabel.
  ///
  /// In en, this message translates to:
  /// **'All levels'**
  String get dictAllLevelsLabel;

  /// No description provided for @dictAllTopicsLabel.
  ///
  /// In en, this message translates to:
  /// **'All topics'**
  String get dictAllTopicsLabel;

  /// No description provided for @dictSearchOfflineBanner.
  ///
  /// In en, this message translates to:
  /// **'Offline — searching cached words and recent history only.'**
  String get dictSearchOfflineBanner;

  /// No description provided for @dictRecentSectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Recent'**
  String get dictRecentSectionTitle;

  /// No description provided for @dictClearAction.
  ///
  /// In en, this message translates to:
  /// **'Clear'**
  String get dictClearAction;

  /// No description provided for @dictBrowseByTopicTitle.
  ///
  /// In en, this message translates to:
  /// **'Browse by topic'**
  String get dictBrowseByTopicTitle;

  /// No description provided for @dictSeeAllAction.
  ///
  /// In en, this message translates to:
  /// **'See all'**
  String get dictSeeAllAction;

  /// No description provided for @dictRecentSearchesEmpty.
  ///
  /// In en, this message translates to:
  /// **'Your recent searches will appear here.'**
  String get dictRecentSearchesEmpty;

  /// No description provided for @dictWordCountLabel.
  ///
  /// In en, this message translates to:
  /// **'{count} words'**
  String dictWordCountLabel(int count);

  /// No description provided for @dictNoMatchingCachedWords.
  ///
  /// In en, this message translates to:
  /// **'No matching cached words'**
  String get dictNoMatchingCachedWords;

  /// No description provided for @dictNoMatchingWords.
  ///
  /// In en, this message translates to:
  /// **'No matching words found'**
  String get dictNoMatchingWords;

  /// No description provided for @dictReconnectHint.
  ///
  /// In en, this message translates to:
  /// **'Reconnect to search the full dictionary.'**
  String get dictReconnectHint;

  /// No description provided for @dictAdjustFiltersHint.
  ///
  /// In en, this message translates to:
  /// **'Try another spelling or adjust the filters.'**
  String get dictAdjustFiltersHint;

  /// No description provided for @dictTopicsTitle.
  ///
  /// In en, this message translates to:
  /// **'Topics'**
  String get dictTopicsTitle;

  /// No description provided for @dictSearchTopicsHint.
  ///
  /// In en, this message translates to:
  /// **'Search topics...'**
  String get dictSearchTopicsHint;

  /// No description provided for @dictNoPersonalTopicsMatch.
  ///
  /// In en, this message translates to:
  /// **'No personal topics match your search.'**
  String get dictNoPersonalTopicsMatch;

  /// No description provided for @dictNoSystemTopics.
  ///
  /// In en, this message translates to:
  /// **'No system topics found.'**
  String get dictNoSystemTopics;

  /// No description provided for @dictSystemLibraryLabel.
  ///
  /// In en, this message translates to:
  /// **'System library'**
  String get dictSystemLibraryLabel;

  /// No description provided for @dictMyTopicsLabel.
  ///
  /// In en, this message translates to:
  /// **'My topics'**
  String get dictMyTopicsLabel;

  /// No description provided for @dictPersonalModeNote.
  ///
  /// In en, this message translates to:
  /// **'Only words you saved appear here, ready for practice.'**
  String get dictPersonalModeNote;

  /// No description provided for @dictSystemModeNote.
  ///
  /// In en, this message translates to:
  /// **'Browse all words organized by the VocaNova team.'**
  String get dictSystemModeNote;

  /// No description provided for @dictPersonalWordCount.
  ///
  /// In en, this message translates to:
  /// **'{count} personal words'**
  String dictPersonalWordCount(int count);

  /// No description provided for @dictSystemWordCount.
  ///
  /// In en, this message translates to:
  /// **'{count} system words'**
  String dictSystemWordCount(int count);

  /// No description provided for @dictUnableToLoadTopics.
  ///
  /// In en, this message translates to:
  /// **'Unable to load topics.'**
  String get dictUnableToLoadTopics;

  /// No description provided for @dictTryAgain.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get dictTryAgain;

  /// No description provided for @dictCategoryAll.
  ///
  /// In en, this message translates to:
  /// **'All'**
  String get dictCategoryAll;

  /// No description provided for @dictCategoryEducation.
  ///
  /// In en, this message translates to:
  /// **'Education'**
  String get dictCategoryEducation;

  /// No description provided for @dictCategoryWork.
  ///
  /// In en, this message translates to:
  /// **'Work'**
  String get dictCategoryWork;

  /// No description provided for @dictCategoryTravel.
  ///
  /// In en, this message translates to:
  /// **'Travel'**
  String get dictCategoryTravel;

  /// No description provided for @dictCategoryDailyLife.
  ///
  /// In en, this message translates to:
  /// **'Daily life'**
  String get dictCategoryDailyLife;

  /// No description provided for @dictMyTopicTitle.
  ///
  /// In en, this message translates to:
  /// **'My {name}'**
  String dictMyTopicTitle(String name);

  /// No description provided for @dictTopicFallbackName.
  ///
  /// In en, this message translates to:
  /// **'topic'**
  String get dictTopicFallbackName;

  /// No description provided for @dictTopicDetailFallbackTitle.
  ///
  /// In en, this message translates to:
  /// **'Topic detail'**
  String get dictTopicDetailFallbackTitle;

  /// No description provided for @dictUnableToLoadWordsRetry.
  ///
  /// In en, this message translates to:
  /// **'Unable to load words. Try again'**
  String get dictUnableToLoadWordsRetry;

  /// No description provided for @dictPracticeSavedWords.
  ///
  /// In en, this message translates to:
  /// **'Practice my saved words'**
  String get dictPracticeSavedWords;

  /// No description provided for @dictNoWordsInCategory.
  ///
  /// In en, this message translates to:
  /// **'No words in this category.'**
  String get dictNoWordsInCategory;

  /// No description provided for @dictRemovedFromTopic.
  ///
  /// In en, this message translates to:
  /// **'Removed from your topic.'**
  String get dictRemovedFromTopic;

  /// No description provided for @dictUnableToRemoveWord.
  ///
  /// In en, this message translates to:
  /// **'Unable to remove this word.'**
  String get dictUnableToRemoveWord;

  /// No description provided for @dictAddToListTooltip.
  ///
  /// In en, this message translates to:
  /// **'Add to list'**
  String get dictAddToListTooltip;

  /// No description provided for @dictRemoveFromTopicTooltip.
  ///
  /// In en, this message translates to:
  /// **'Remove from my topic'**
  String get dictRemoveFromTopicTooltip;

  /// No description provided for @dictStatMastered.
  ///
  /// In en, this message translates to:
  /// **'Mastered'**
  String get dictStatMastered;

  /// No description provided for @dictStatLearning.
  ///
  /// In en, this message translates to:
  /// **'Learning'**
  String get dictStatLearning;

  /// No description provided for @dictStatNew.
  ///
  /// In en, this message translates to:
  /// **'New'**
  String get dictStatNew;

  /// No description provided for @dictStatAvgMastery.
  ///
  /// In en, this message translates to:
  /// **'Avg mastery'**
  String get dictStatAvgMastery;

  /// No description provided for @dictWordNotFound.
  ///
  /// In en, this message translates to:
  /// **'Word not found.'**
  String get dictWordNotFound;

  /// No description provided for @dictDefinitionLabel.
  ///
  /// In en, this message translates to:
  /// **'Definition'**
  String get dictDefinitionLabel;

  /// No description provided for @dictVietnameseMeaningLabel.
  ///
  /// In en, this message translates to:
  /// **'Vietnamese'**
  String get dictVietnameseMeaningLabel;

  /// No description provided for @dictUnableToPlayAudio.
  ///
  /// In en, this message translates to:
  /// **'Unable to play audio.'**
  String get dictUnableToPlayAudio;

  /// No description provided for @dictWordCopied.
  ///
  /// In en, this message translates to:
  /// **'Word copied to clipboard.'**
  String get dictWordCopied;

  /// No description provided for @dictShareTooltip.
  ///
  /// In en, this message translates to:
  /// **'Share'**
  String get dictShareTooltip;

  /// No description provided for @dictSavedTooltip.
  ///
  /// In en, this message translates to:
  /// **'Saved'**
  String get dictSavedTooltip;

  /// No description provided for @dictSaveWordTooltip.
  ///
  /// In en, this message translates to:
  /// **'Save word'**
  String get dictSaveWordTooltip;

  /// No description provided for @dictExampleLabel.
  ///
  /// In en, this message translates to:
  /// **'Example'**
  String get dictExampleLabel;

  /// No description provided for @dictSynonymsLabel.
  ///
  /// In en, this message translates to:
  /// **'Synonyms'**
  String get dictSynonymsLabel;

  /// No description provided for @dictAntonymsLabel.
  ///
  /// In en, this message translates to:
  /// **'Antonyms'**
  String get dictAntonymsLabel;

  /// No description provided for @dictPracticeLabel.
  ///
  /// In en, this message translates to:
  /// **'Practice'**
  String get dictPracticeLabel;

  /// No description provided for @dictWordDetailOfflineBanner.
  ///
  /// In en, this message translates to:
  /// **'Offline — showing saved word details.'**
  String get dictWordDetailOfflineBanner;

  /// No description provided for @dictAddToListSheetSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Choose a personal list or build a quiz-ready topic.'**
  String get dictAddToListSheetSubtitle;

  /// No description provided for @dictMyListsLabel.
  ///
  /// In en, this message translates to:
  /// **'My lists'**
  String get dictMyListsLabel;

  /// No description provided for @dictNewListLabel.
  ///
  /// In en, this message translates to:
  /// **'New list'**
  String get dictNewListLabel;

  /// No description provided for @dictNoteOptionalLabel.
  ///
  /// In en, this message translates to:
  /// **'Note (optional)'**
  String get dictNoteOptionalLabel;

  /// No description provided for @dictAddNoteHint.
  ///
  /// In en, this message translates to:
  /// **'Add a note...'**
  String get dictAddNoteHint;

  /// No description provided for @dictSaveToListLabel.
  ///
  /// In en, this message translates to:
  /// **'Save to list'**
  String get dictSaveToListLabel;

  /// No description provided for @dictAddToTopicLabel.
  ///
  /// In en, this message translates to:
  /// **'Add to topic'**
  String get dictAddToTopicLabel;

  /// No description provided for @dictCreateListPrompt.
  ///
  /// In en, this message translates to:
  /// **'Create a list to save this word.'**
  String get dictCreateListPrompt;

  /// No description provided for @dictNoSystemTopicAssignment.
  ///
  /// In en, this message translates to:
  /// **'This word has no system topic assignment.'**
  String get dictNoSystemTopicAssignment;

  /// No description provided for @dictAlreadyInTopic.
  ///
  /// In en, this message translates to:
  /// **'Already in your topic'**
  String get dictAlreadyInTopic;

  /// No description provided for @dictUnableToLoadDestinations.
  ///
  /// In en, this message translates to:
  /// **'Unable to load your save destinations.'**
  String get dictUnableToLoadDestinations;

  /// No description provided for @dictListNameHint.
  ///
  /// In en, this message translates to:
  /// **'List name'**
  String get dictListNameHint;

  /// No description provided for @dictCancelLabel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get dictCancelLabel;

  /// No description provided for @dictCreateLabel.
  ///
  /// In en, this message translates to:
  /// **'Create'**
  String get dictCreateLabel;

  /// No description provided for @dictUnableToCreateList.
  ///
  /// In en, this message translates to:
  /// **'Unable to create the list.'**
  String get dictUnableToCreateList;

  /// No description provided for @dictAddedToDestination.
  ///
  /// In en, this message translates to:
  /// **'Added to {name}.'**
  String dictAddedToDestination(String name);

  /// No description provided for @dictUnableToSaveWord.
  ///
  /// In en, this message translates to:
  /// **'Unable to save this word.'**
  String get dictUnableToSaveWord;

  /// No description provided for @dictNoSavedWordDataError.
  ///
  /// In en, this message translates to:
  /// **'No saved word data available.'**
  String get dictNoSavedWordDataError;

  /// No description provided for @dictWordDetailLoadError.
  ///
  /// In en, this message translates to:
  /// **'Unable to load word details.'**
  String get dictWordDetailLoadError;

  /// No description provided for @dictSearchRefreshError.
  ///
  /// In en, this message translates to:
  /// **'Unable to load new data. Showing saved words.'**
  String get dictSearchRefreshError;

  /// No description provided for @listsTitle.
  ///
  /// In en, this message translates to:
  /// **'My Word Lists'**
  String get listsTitle;

  /// No description provided for @listsCreateDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Create list'**
  String get listsCreateDialogTitle;

  /// No description provided for @listsRenameDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Rename list'**
  String get listsRenameDialogTitle;

  /// No description provided for @listsMyListsSection.
  ///
  /// In en, this message translates to:
  /// **'My lists'**
  String get listsMyListsSection;

  /// No description provided for @listsPersonalTopicsSection.
  ///
  /// In en, this message translates to:
  /// **'Personal topics'**
  String get listsPersonalTopicsSection;

  /// No description provided for @listsNameFieldHint.
  ///
  /// In en, this message translates to:
  /// **'List name'**
  String get listsNameFieldHint;

  /// No description provided for @listsCancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get listsCancel;

  /// No description provided for @listsCreateAction.
  ///
  /// In en, this message translates to:
  /// **'Create'**
  String get listsCreateAction;

  /// No description provided for @listsSaveAction.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get listsSaveAction;

  /// No description provided for @listsRenameAction.
  ///
  /// In en, this message translates to:
  /// **'Rename'**
  String get listsRenameAction;

  /// No description provided for @listsDeleteAction.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get listsDeleteAction;

  /// No description provided for @listsDeleteConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete list?'**
  String get listsDeleteConfirmTitle;

  /// No description provided for @listsDeleteConfirmBody.
  ///
  /// In en, this message translates to:
  /// **'The list \"{name}\" will be deleted.'**
  String listsDeleteConfirmBody(String name);

  /// No description provided for @listsNameRequiredError.
  ///
  /// In en, this message translates to:
  /// **'Please enter a list name.'**
  String get listsNameRequiredError;

  /// No description provided for @listsNameMaxLengthError.
  ///
  /// In en, this message translates to:
  /// **'List name can be at most 100 characters.'**
  String get listsNameMaxLengthError;

  /// No description provided for @listsWordCount.
  ///
  /// In en, this message translates to:
  /// **'{count} words'**
  String listsWordCount(int count);

  /// No description provided for @listsCreatedOnLabel.
  ///
  /// In en, this message translates to:
  /// **'Created on {date}'**
  String listsCreatedOnLabel(String date);

  /// No description provided for @listsOfflineBanner.
  ///
  /// In en, this message translates to:
  /// **'You\'re offline. Showing saved lists.'**
  String get listsOfflineBanner;

  /// No description provided for @listsEmptyState.
  ///
  /// In en, this message translates to:
  /// **'You don\'t have any word lists yet.\nTap + to create your first list.'**
  String get listsEmptyState;

  /// No description provided for @listsDetailTitleFallback.
  ///
  /// In en, this message translates to:
  /// **'List details'**
  String get listsDetailTitleFallback;

  /// No description provided for @listsAddWordAction.
  ///
  /// In en, this message translates to:
  /// **'Add word'**
  String get listsAddWordAction;

  /// No description provided for @listsDetailEmpty.
  ///
  /// In en, this message translates to:
  /// **'This list doesn\'t have any words yet.'**
  String get listsDetailEmpty;

  /// No description provided for @listsRemoveWordConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'Remove word?'**
  String get listsRemoveWordConfirmTitle;

  /// No description provided for @listsRemoveWordConfirmBody.
  ///
  /// In en, this message translates to:
  /// **'Remove \"{word}\" from the list?'**
  String listsRemoveWordConfirmBody(String word);

  /// No description provided for @listsLoadTopicsError.
  ///
  /// In en, this message translates to:
  /// **'Unable to load topics.'**
  String get listsLoadTopicsError;

  /// No description provided for @listsAddRandomDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Add random words'**
  String get listsAddRandomDialogTitle;

  /// No description provided for @listsByTopicOption.
  ///
  /// In en, this message translates to:
  /// **'By topic'**
  String get listsByTopicOption;

  /// No description provided for @listsSynonymOption.
  ///
  /// In en, this message translates to:
  /// **'Synonyms'**
  String get listsSynonymOption;

  /// No description provided for @listsAntonymOption.
  ///
  /// In en, this message translates to:
  /// **'Antonyms'**
  String get listsAntonymOption;

  /// No description provided for @listsCountFieldLabel.
  ///
  /// In en, this message translates to:
  /// **'Count (1-50)'**
  String get listsCountFieldLabel;

  /// No description provided for @listsAddAction.
  ///
  /// In en, this message translates to:
  /// **'Add'**
  String get listsAddAction;

  /// No description provided for @listsAddRandomAction.
  ///
  /// In en, this message translates to:
  /// **'Add random'**
  String get listsAddRandomAction;

  /// No description provided for @listsStartQuizAction.
  ///
  /// In en, this message translates to:
  /// **'Start quiz'**
  String get listsStartQuizAction;

  /// No description provided for @listsCorrectCount.
  ///
  /// In en, this message translates to:
  /// **'Correct: {count}'**
  String listsCorrectCount(int count);

  /// No description provided for @listsWrongCount.
  ///
  /// In en, this message translates to:
  /// **'Wrong: {count}'**
  String listsWrongCount(int count);

  /// No description provided for @listsNoteLabel.
  ///
  /// In en, this message translates to:
  /// **'Note: {note}'**
  String listsNoteLabel(String note);

  /// No description provided for @listsDetailOfflineBanner.
  ///
  /// In en, this message translates to:
  /// **'You\'re offline. Showing saved words.'**
  String get listsDetailOfflineBanner;

  /// No description provided for @listsSearchWordHint.
  ///
  /// In en, this message translates to:
  /// **'Search for an English word'**
  String get listsSearchWordHint;

  /// No description provided for @listsLoadListsError.
  ///
  /// In en, this message translates to:
  /// **'Unable to load word lists.'**
  String get listsLoadListsError;

  /// No description provided for @listsCreateError.
  ///
  /// In en, this message translates to:
  /// **'Unable to create list. The name may already exist.'**
  String get listsCreateError;

  /// No description provided for @listsRenameError.
  ///
  /// In en, this message translates to:
  /// **'Unable to rename list.'**
  String get listsRenameError;

  /// No description provided for @listsDeleteError.
  ///
  /// In en, this message translates to:
  /// **'Unable to delete list.'**
  String get listsDeleteError;

  /// No description provided for @listsOfflineMutateError.
  ///
  /// In en, this message translates to:
  /// **'An internet connection is required to change lists.'**
  String get listsOfflineMutateError;

  /// No description provided for @listsLoadWordsError.
  ///
  /// In en, this message translates to:
  /// **'Unable to load words in this list.'**
  String get listsLoadWordsError;

  /// No description provided for @listsLoadMoreWordsError.
  ///
  /// In en, this message translates to:
  /// **'Unable to load more words.'**
  String get listsLoadMoreWordsError;

  /// No description provided for @listsAddWordError.
  ///
  /// In en, this message translates to:
  /// **'Unable to add word to the list.'**
  String get listsAddWordError;

  /// No description provided for @listsAddRandomError.
  ///
  /// In en, this message translates to:
  /// **'Unable to add random words.'**
  String get listsAddRandomError;

  /// No description provided for @listsRemoveWordError.
  ///
  /// In en, this message translates to:
  /// **'Unable to remove word from the list.'**
  String get listsRemoveWordError;

  /// No description provided for @progressOverviewTitle.
  ///
  /// In en, this message translates to:
  /// **'Learning Progress'**
  String get progressOverviewTitle;

  /// No description provided for @progressChartsTooltip.
  ///
  /// In en, this message translates to:
  /// **'Detailed charts'**
  String get progressChartsTooltip;

  /// No description provided for @progressStreakDaysLabel.
  ///
  /// In en, this message translates to:
  /// **'{days} days in a row'**
  String progressStreakDaysLabel(int days);

  /// No description provided for @progressLongestStreakLabel.
  ///
  /// In en, this message translates to:
  /// **'Record: {days} days'**
  String progressLongestStreakLabel(int days);

  /// No description provided for @progressAccuracy7DaysLabel.
  ///
  /// In en, this message translates to:
  /// **'7-day accuracy'**
  String get progressAccuracy7DaysLabel;

  /// No description provided for @progressCorrectAnswersLabel.
  ///
  /// In en, this message translates to:
  /// **'{correct}/{total} correct'**
  String progressCorrectAnswersLabel(int correct, int total);

  /// No description provided for @progressWordsInProgressLabel.
  ///
  /// In en, this message translates to:
  /// **'Words in progress'**
  String get progressWordsInProgressLabel;

  /// No description provided for @progressMasteredWordsLabel.
  ///
  /// In en, this message translates to:
  /// **'Mastered words'**
  String get progressMasteredWordsLabel;

  /// No description provided for @progressSessionsThisMonthLabel.
  ///
  /// In en, this message translates to:
  /// **'Quizzes this month'**
  String get progressSessionsThisMonthLabel;

  /// No description provided for @progressOfflineBanner.
  ///
  /// In en, this message translates to:
  /// **'You\'re offline. Showing saved progress data.'**
  String get progressOfflineBanner;

  /// No description provided for @progressNoDataMessage.
  ///
  /// In en, this message translates to:
  /// **'No progress data yet.'**
  String get progressNoDataMessage;

  /// No description provided for @progressRetry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get progressRetry;

  /// No description provided for @progressChartsTitle.
  ///
  /// In en, this message translates to:
  /// **'Progress charts'**
  String get progressChartsTitle;

  /// No description provided for @progressGranularityDaily.
  ///
  /// In en, this message translates to:
  /// **'Daily'**
  String get progressGranularityDaily;

  /// No description provided for @progressGranularityWeekly.
  ///
  /// In en, this message translates to:
  /// **'Weekly'**
  String get progressGranularityWeekly;

  /// No description provided for @progressGranularityMonthly.
  ///
  /// In en, this message translates to:
  /// **'Monthly'**
  String get progressGranularityMonthly;

  /// No description provided for @progressSessionsCountLabel.
  ///
  /// In en, this message translates to:
  /// **'Number of sessions'**
  String get progressSessionsCountLabel;

  /// No description provided for @progressMasteryLevelLabel.
  ///
  /// In en, this message translates to:
  /// **'Mastery level'**
  String get progressMasteryLevelLabel;

  /// No description provided for @progressTop10WeakestWordsLabel.
  ///
  /// In en, this message translates to:
  /// **'10 weakest words'**
  String get progressTop10WeakestWordsLabel;

  /// No description provided for @progressNoWeakestWords.
  ///
  /// In en, this message translates to:
  /// **'No weak words to review yet.'**
  String get progressNoWeakestWords;

  /// No description provided for @progressMasteryLevelShort.
  ///
  /// In en, this message translates to:
  /// **'Lv.{level}'**
  String progressMasteryLevelShort(int level);

  /// No description provided for @progressWordStatsLabel.
  ///
  /// In en, this message translates to:
  /// **'Correct {correct} · Wrong {wrong}'**
  String progressWordStatsLabel(int correct, int wrong);

  /// No description provided for @progressNoCachedDataError.
  ///
  /// In en, this message translates to:
  /// **'No saved progress data.'**
  String get progressNoCachedDataError;

  /// No description provided for @progressLoadOverviewError.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t load progress overview.'**
  String get progressLoadOverviewError;

  /// No description provided for @progressLoadChartsError.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t load progress charts.'**
  String get progressLoadChartsError;

  /// No description provided for @progressChangeGranularityError.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t change the chart\'s time range.'**
  String get progressChangeGranularityError;

  /// No description provided for @homeWelcomeBack.
  ///
  /// In en, this message translates to:
  /// **'Welcome back'**
  String get homeWelcomeBack;

  /// No description provided for @homeGreetingName.
  ///
  /// In en, this message translates to:
  /// **'Hi, {name}'**
  String homeGreetingName(String name);

  /// No description provided for @homeGreetingMorning.
  ///
  /// In en, this message translates to:
  /// **'Good morning'**
  String get homeGreetingMorning;

  /// No description provided for @homeGreetingAfternoon.
  ///
  /// In en, this message translates to:
  /// **'Good afternoon'**
  String get homeGreetingAfternoon;

  /// No description provided for @homeGreetingEvening.
  ///
  /// In en, this message translates to:
  /// **'Good evening'**
  String get homeGreetingEvening;

  /// No description provided for @homeSeeAll.
  ///
  /// In en, this message translates to:
  /// **'See all'**
  String get homeSeeAll;

  /// No description provided for @homeSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Search a word...'**
  String get homeSearchHint;

  /// No description provided for @homeDailyGoalLabel.
  ///
  /// In en, this message translates to:
  /// **'DAILY GOAL'**
  String get homeDailyGoalLabel;

  /// No description provided for @homeGoalProgress.
  ///
  /// In en, this message translates to:
  /// **'{mastered} / {total} words'**
  String homeGoalProgress(int mastered, int total);

  /// No description provided for @homeMasteredSoFar.
  ///
  /// In en, this message translates to:
  /// **'mastered so far'**
  String get homeMasteredSoFar;

  /// No description provided for @homeStreakActive.
  ///
  /// In en, this message translates to:
  /// **'Keep your {days}-day streak'**
  String homeStreakActive(int days);

  /// No description provided for @homeStreakInactive.
  ///
  /// In en, this message translates to:
  /// **'Start a streak today'**
  String get homeStreakInactive;

  /// No description provided for @homeWordOfTheDayLabel.
  ///
  /// In en, this message translates to:
  /// **'WORD OF THE DAY'**
  String get homeWordOfTheDayLabel;

  /// No description provided for @homeLearnThisWord.
  ///
  /// In en, this message translates to:
  /// **'Learn this word'**
  String get homeLearnThisWord;

  /// No description provided for @homeDailyWordLoading.
  ///
  /// In en, this message translates to:
  /// **'Loading…'**
  String get homeDailyWordLoading;

  /// No description provided for @homeDailyWordUnavailable.
  ///
  /// In en, this message translates to:
  /// **'Unavailable'**
  String get homeDailyWordUnavailable;

  /// No description provided for @homeDailyWordChoosing.
  ///
  /// In en, this message translates to:
  /// **'Choosing a word from your dictionary…'**
  String get homeDailyWordChoosing;

  /// No description provided for @homeDailyWordLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load today’s word. Pull down to retry.'**
  String get homeDailyWordLoadError;

  /// No description provided for @homePronunciationPlayTooltip.
  ///
  /// In en, this message translates to:
  /// **'Play pronunciation'**
  String get homePronunciationPlayTooltip;

  /// No description provided for @homePronunciationPlayError.
  ///
  /// In en, this message translates to:
  /// **'Unable to play pronunciation.'**
  String get homePronunciationPlayError;

  /// No description provided for @homeStatWords.
  ///
  /// In en, this message translates to:
  /// **'Words'**
  String get homeStatWords;

  /// No description provided for @homeStatAccuracy.
  ///
  /// In en, this message translates to:
  /// **'Accuracy'**
  String get homeStatAccuracy;

  /// No description provided for @homeStatMastered.
  ///
  /// In en, this message translates to:
  /// **'Mastered'**
  String get homeStatMastered;

  /// No description provided for @homeContinueLabel.
  ///
  /// In en, this message translates to:
  /// **'CONTINUE'**
  String get homeContinueLabel;

  /// No description provided for @homeWordCount.
  ///
  /// In en, this message translates to:
  /// **'{count,plural, =1{{count} word} other{{count} words}}'**
  String homeWordCount(int count);

  /// No description provided for @homeQuickActionsTitle.
  ///
  /// In en, this message translates to:
  /// **'Quick actions'**
  String get homeQuickActionsTitle;

  /// No description provided for @homeActionQuiz.
  ///
  /// In en, this message translates to:
  /// **'Quiz'**
  String get homeActionQuiz;

  /// No description provided for @homeActionReview.
  ///
  /// In en, this message translates to:
  /// **'Review'**
  String get homeActionReview;

  /// No description provided for @homeActionTopics.
  ///
  /// In en, this message translates to:
  /// **'Topics'**
  String get homeActionTopics;

  /// No description provided for @homeTopicsForYouTitle.
  ///
  /// In en, this message translates to:
  /// **'Topics for you'**
  String get homeTopicsForYouTitle;

  /// No description provided for @homeTopicsForYouEmpty.
  ///
  /// In en, this message translates to:
  /// **'Your next collection is taking shape'**
  String get homeTopicsForYouEmpty;

  /// No description provided for @homeExploreTopics.
  ///
  /// In en, this message translates to:
  /// **'Explore topics'**
  String get homeExploreTopics;

  /// No description provided for @homeTopicWordCount.
  ///
  /// In en, this message translates to:
  /// **'{count,plural, =1{{count} word} other{{count} words}}'**
  String homeTopicWordCount(int count);

  /// No description provided for @homeWordsToReviewLabel.
  ///
  /// In en, this message translates to:
  /// **'{count} words to review'**
  String homeWordsToReviewLabel(String count);

  /// No description provided for @homeTapToReviewMistakes.
  ///
  /// In en, this message translates to:
  /// **'Tap to review your mistakes'**
  String get homeTapToReviewMistakes;

  /// No description provided for @homeMyListsTitle.
  ///
  /// In en, this message translates to:
  /// **'My lists'**
  String get homeMyListsTitle;

  /// No description provided for @homeCreateFirstList.
  ///
  /// In en, this message translates to:
  /// **'Create your first list to start learning'**
  String get homeCreateFirstList;

  /// No description provided for @homeWeekdayMonShort.
  ///
  /// In en, this message translates to:
  /// **'M'**
  String get homeWeekdayMonShort;

  /// No description provided for @homeWeekdayTueShort.
  ///
  /// In en, this message translates to:
  /// **'T'**
  String get homeWeekdayTueShort;

  /// No description provided for @homeWeekdayWedShort.
  ///
  /// In en, this message translates to:
  /// **'W'**
  String get homeWeekdayWedShort;

  /// No description provided for @homeWeekdayThuShort.
  ///
  /// In en, this message translates to:
  /// **'T'**
  String get homeWeekdayThuShort;

  /// No description provided for @homeWeekdayFriShort.
  ///
  /// In en, this message translates to:
  /// **'F'**
  String get homeWeekdayFriShort;

  /// No description provided for @homeWeekdaySatShort.
  ///
  /// In en, this message translates to:
  /// **'S'**
  String get homeWeekdaySatShort;

  /// No description provided for @homeWeekdaySunShort.
  ///
  /// In en, this message translates to:
  /// **'S'**
  String get homeWeekdaySunShort;

  /// No description provided for @notifTitle.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get notifTitle;

  /// No description provided for @notifMarkAllRead.
  ///
  /// In en, this message translates to:
  /// **'Mark all read'**
  String get notifMarkAllRead;

  /// No description provided for @notifEmptyMessage.
  ///
  /// In en, this message translates to:
  /// **'No notifications yet.'**
  String get notifEmptyMessage;

  /// No description provided for @notifRetry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get notifRetry;

  /// No description provided for @notifClose.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get notifClose;

  /// No description provided for @notifJustNow.
  ///
  /// In en, this message translates to:
  /// **'Just now'**
  String get notifJustNow;

  /// No description provided for @notifMinutesAgo.
  ///
  /// In en, this message translates to:
  /// **'{count} min ago'**
  String notifMinutesAgo(int count);

  /// No description provided for @notifHoursAgo.
  ///
  /// In en, this message translates to:
  /// **'{count} hr ago'**
  String notifHoursAgo(int count);

  /// No description provided for @notifDaysAgo.
  ///
  /// In en, this message translates to:
  /// **'{count} days ago'**
  String notifDaysAgo(int count);

  /// No description provided for @notifLoadError.
  ///
  /// In en, this message translates to:
  /// **'Couldn\'t load notifications.'**
  String get notifLoadError;

  /// No description provided for @quizConfigTitle.
  ///
  /// In en, this message translates to:
  /// **'Quiz setup'**
  String get quizConfigTitle;

  /// No description provided for @quizConfigScopeSection.
  ///
  /// In en, this message translates to:
  /// **'Word scope'**
  String get quizConfigScopeSection;

  /// No description provided for @quizConfigScopeAll.
  ///
  /// In en, this message translates to:
  /// **'All'**
  String get quizConfigScopeAll;

  /// No description provided for @quizConfigScopeFromDate.
  ///
  /// In en, this message translates to:
  /// **'From date'**
  String get quizConfigScopeFromDate;

  /// No description provided for @quizConfigScopeToDate.
  ///
  /// In en, this message translates to:
  /// **'To date'**
  String get quizConfigScopeToDate;

  /// No description provided for @quizConfigScopeDateRange.
  ///
  /// In en, this message translates to:
  /// **'Date range'**
  String get quizConfigScopeDateRange;

  /// No description provided for @quizConfigScopeWrongWords.
  ///
  /// In en, this message translates to:
  /// **'Recently wrong words'**
  String get quizConfigScopeWrongWords;

  /// No description provided for @quizConfigDateFrom.
  ///
  /// In en, this message translates to:
  /// **'Start date'**
  String get quizConfigDateFrom;

  /// No description provided for @quizConfigDateTo.
  ///
  /// In en, this message translates to:
  /// **'End date'**
  String get quizConfigDateTo;

  /// No description provided for @quizConfigSourceSection.
  ///
  /// In en, this message translates to:
  /// **'Quiz source'**
  String get quizConfigSourceSection;

  /// No description provided for @quizConfigModeSection.
  ///
  /// In en, this message translates to:
  /// **'Mode'**
  String get quizConfigModeSection;

  /// No description provided for @quizConfigQuestionTypeSection.
  ///
  /// In en, this message translates to:
  /// **'Question type'**
  String get quizConfigQuestionTypeSection;

  /// No description provided for @quizConfigAnswerMethodSection.
  ///
  /// In en, this message translates to:
  /// **'Answer method'**
  String get quizConfigAnswerMethodSection;

  /// No description provided for @quizConfigWordOrderSection.
  ///
  /// In en, this message translates to:
  /// **'Order'**
  String get quizConfigWordOrderSection;

  /// No description provided for @quizConfigQuestionLimitSection.
  ///
  /// In en, this message translates to:
  /// **'Question count'**
  String get quizConfigQuestionLimitSection;

  /// No description provided for @quizConfigNeedConnection.
  ///
  /// In en, this message translates to:
  /// **'Internet connection required'**
  String get quizConfigNeedConnection;

  /// No description provided for @quizConfigStartButton.
  ///
  /// In en, this message translates to:
  /// **'Start'**
  String get quizConfigStartButton;

  /// No description provided for @quizConfigQuestionTypeWordToMeaning.
  ///
  /// In en, this message translates to:
  /// **'Word → meaning'**
  String get quizConfigQuestionTypeWordToMeaning;

  /// No description provided for @quizConfigQuestionTypeMeaningToWord.
  ///
  /// In en, this message translates to:
  /// **'Meaning → word'**
  String get quizConfigQuestionTypeMeaningToWord;

  /// No description provided for @quizConfigQuestionTypeDescToWord.
  ///
  /// In en, this message translates to:
  /// **'Description → word'**
  String get quizConfigQuestionTypeDescToWord;

  /// No description provided for @quizAnswerMultipleChoice.
  ///
  /// In en, this message translates to:
  /// **'Multiple choice'**
  String get quizAnswerMultipleChoice;

  /// No description provided for @quizAnswerTyping.
  ///
  /// In en, this message translates to:
  /// **'Typing'**
  String get quizAnswerTyping;

  /// No description provided for @quizAnswerAiTyping.
  ///
  /// In en, this message translates to:
  /// **'AI typing'**
  String get quizAnswerAiTyping;

  /// No description provided for @quizConfigOrderRandom.
  ///
  /// In en, this message translates to:
  /// **'Random'**
  String get quizConfigOrderRandom;

  /// No description provided for @quizConfigOrderNewest.
  ///
  /// In en, this message translates to:
  /// **'Newest'**
  String get quizConfigOrderNewest;

  /// No description provided for @quizConfigOrderOldest.
  ///
  /// In en, this message translates to:
  /// **'Oldest'**
  String get quizConfigOrderOldest;

  /// No description provided for @quizConfigOrderByDifficulty.
  ///
  /// In en, this message translates to:
  /// **'By difficulty'**
  String get quizConfigOrderByDifficulty;

  /// No description provided for @quizModeStandard.
  ///
  /// In en, this message translates to:
  /// **'Standard'**
  String get quizModeStandard;

  /// No description provided for @quizModeTimed.
  ///
  /// In en, this message translates to:
  /// **'Timed'**
  String get quizModeTimed;

  /// No description provided for @quizModeChallenge.
  ///
  /// In en, this message translates to:
  /// **'Challenge'**
  String get quizModeChallenge;

  /// No description provided for @quizModeElimination.
  ///
  /// In en, this message translates to:
  /// **'Elimination'**
  String get quizModeElimination;

  /// No description provided for @quizConfigModeStandardSubtitle.
  ///
  /// In en, this message translates to:
  /// **'At your own pace'**
  String get quizConfigModeStandardSubtitle;

  /// No description provided for @quizConfigModeTimedSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Race against the clock'**
  String get quizConfigModeTimedSubtitle;

  /// No description provided for @quizConfigModeChallengeSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Lives and correct streaks'**
  String get quizConfigModeChallengeSubtitle;

  /// No description provided for @quizConfigModeEliminationSubtitle.
  ///
  /// In en, this message translates to:
  /// **'One mistake ends it'**
  String get quizConfigModeEliminationSubtitle;

  /// No description provided for @quizConfigTimeLimitLabel.
  ///
  /// In en, this message translates to:
  /// **'Time limit (seconds)'**
  String get quizConfigTimeLimitLabel;

  /// No description provided for @quizConfigLivesLabel.
  ///
  /// In en, this message translates to:
  /// **'Lives'**
  String get quizConfigLivesLabel;

  /// No description provided for @quizConfigLimitAll.
  ///
  /// In en, this message translates to:
  /// **'All'**
  String get quizConfigLimitAll;

  /// No description provided for @quizConfigLimitCustom.
  ///
  /// In en, this message translates to:
  /// **'Custom'**
  String get quizConfigLimitCustom;

  /// No description provided for @quizConfigCustomLimitLabel.
  ///
  /// In en, this message translates to:
  /// **'Desired question count'**
  String get quizConfigCustomLimitLabel;

  /// No description provided for @quizConfigCustomLimitHelper.
  ///
  /// In en, this message translates to:
  /// **'Source has {wordCount} words · a higher value will be capped at {wordCount}'**
  String quizConfigCustomLimitHelper(int wordCount);

  /// No description provided for @quizConfigCustomLimitHelperEmpty.
  ///
  /// In en, this message translates to:
  /// **'Enter the desired question count'**
  String get quizConfigCustomLimitHelperEmpty;

  /// No description provided for @quizConfigSourceMyList.
  ///
  /// In en, this message translates to:
  /// **'My lists'**
  String get quizConfigSourceMyList;

  /// No description provided for @quizConfigSourcePersonalTopic.
  ///
  /// In en, this message translates to:
  /// **'Personal topics'**
  String get quizConfigSourcePersonalTopic;

  /// No description provided for @quizConfigSourceHintList.
  ///
  /// In en, this message translates to:
  /// **'Choose one of your word lists.'**
  String get quizConfigSourceHintList;

  /// No description provided for @quizConfigSourceHintTopic.
  ///
  /// In en, this message translates to:
  /// **'Choose a topic containing your saved words.'**
  String get quizConfigSourceHintTopic;

  /// No description provided for @quizConfigSourceNoWords.
  ///
  /// In en, this message translates to:
  /// **'No words to quiz yet'**
  String get quizConfigSourceNoWords;

  /// No description provided for @quizConfigSourceWordCount.
  ///
  /// In en, this message translates to:
  /// **'{count} words'**
  String quizConfigSourceWordCount(int count);

  /// No description provided for @quizConfigEmptyTopicHint.
  ///
  /// In en, this message translates to:
  /// **'Save some words to a personal topic before taking a quiz.'**
  String get quizConfigEmptyTopicHint;

  /// No description provided for @quizConfigEmptyListHint.
  ///
  /// In en, this message translates to:
  /// **'Create a list and add words before taking a quiz.'**
  String get quizConfigEmptyListHint;

  /// No description provided for @quizConfigSummaryMode.
  ///
  /// In en, this message translates to:
  /// **'Mode'**
  String get quizConfigSummaryMode;

  /// No description provided for @quizConfigSummaryAnswer.
  ///
  /// In en, this message translates to:
  /// **'Answer'**
  String get quizConfigSummaryAnswer;

  /// No description provided for @quizConfigSummaryCount.
  ///
  /// In en, this message translates to:
  /// **'Questions'**
  String get quizConfigSummaryCount;

  /// No description provided for @quizConfigLoadSourcesError.
  ///
  /// In en, this message translates to:
  /// **'Could not load quiz sources.'**
  String get quizConfigLoadSourcesError;

  /// No description provided for @quizConfigValidateNoSource.
  ///
  /// In en, this message translates to:
  /// **'Please choose a list or personal topic to quiz on.'**
  String get quizConfigValidateNoSource;

  /// No description provided for @quizConfigValidateNoQuestionCount.
  ///
  /// In en, this message translates to:
  /// **'Please enter the number of questions.'**
  String get quizConfigValidateNoQuestionCount;

  /// No description provided for @quizConfigValidateQuestionCountPositive.
  ///
  /// In en, this message translates to:
  /// **'The number of questions must be greater than 0.'**
  String get quizConfigValidateQuestionCountPositive;

  /// No description provided for @quizConfigValidateDateFromRequired.
  ///
  /// In en, this message translates to:
  /// **'Please choose a start date.'**
  String get quizConfigValidateDateFromRequired;

  /// No description provided for @quizConfigValidateDateToRequired.
  ///
  /// In en, this message translates to:
  /// **'Please choose an end date.'**
  String get quizConfigValidateDateToRequired;

  /// No description provided for @quizConfigValidateDateOrder.
  ///
  /// In en, this message translates to:
  /// **'The start date must be before or equal to the end date.'**
  String get quizConfigValidateDateOrder;

  /// No description provided for @quizConfigValidateTimedPositive.
  ///
  /// In en, this message translates to:
  /// **'Timed mode needs a time limit greater than 0.'**
  String get quizConfigValidateTimedPositive;

  /// No description provided for @quizConfigValidateEliminationPositive.
  ///
  /// In en, this message translates to:
  /// **'Elimination mode needs more than 0 lives.'**
  String get quizConfigValidateEliminationPositive;

  /// No description provided for @quizConfigCreateSessionError.
  ///
  /// In en, this message translates to:
  /// **'Could not create the quiz. Please check the word count.'**
  String get quizConfigCreateSessionError;

  /// No description provided for @quizSessionTitle.
  ///
  /// In en, this message translates to:
  /// **'Quiz'**
  String get quizSessionTitle;

  /// No description provided for @quizSessionAbandonTooltip.
  ///
  /// In en, this message translates to:
  /// **'Quit quiz'**
  String get quizSessionAbandonTooltip;

  /// No description provided for @quizSessionQuestionNumber.
  ///
  /// In en, this message translates to:
  /// **'Question {number}'**
  String quizSessionQuestionNumber(int number);

  /// No description provided for @quizSessionFinishing.
  ///
  /// In en, this message translates to:
  /// **'Finishing...'**
  String get quizSessionFinishing;

  /// No description provided for @quizSessionViewResult.
  ///
  /// In en, this message translates to:
  /// **'View result'**
  String get quizSessionViewResult;

  /// No description provided for @quizSessionNext.
  ///
  /// In en, this message translates to:
  /// **'Next'**
  String get quizSessionNext;

  /// No description provided for @quizSessionAbandonDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Quit the quiz?'**
  String get quizSessionAbandonDialogTitle;

  /// No description provided for @quizSessionAbandonDialogContent.
  ///
  /// In en, this message translates to:
  /// **'Your current progress will be ended and saved.'**
  String get quizSessionAbandonDialogContent;

  /// No description provided for @quizSessionAbandonCancel.
  ///
  /// In en, this message translates to:
  /// **'Keep going'**
  String get quizSessionAbandonCancel;

  /// No description provided for @quizSessionAbandonConfirm.
  ///
  /// In en, this message translates to:
  /// **'Quit'**
  String get quizSessionAbandonConfirm;

  /// No description provided for @quizSessionProgressLabel.
  ///
  /// In en, this message translates to:
  /// **'Question {current}/{total}'**
  String quizSessionProgressLabel(int current, int total);

  /// No description provided for @quizSessionUnavailableMessage.
  ///
  /// In en, this message translates to:
  /// **'Could not restore the running quiz. Please start a new one.'**
  String get quizSessionUnavailableMessage;

  /// No description provided for @quizSessionCreateNew.
  ///
  /// In en, this message translates to:
  /// **'Create a quiz'**
  String get quizSessionCreateNew;

  /// No description provided for @quizTypingLabelAi.
  ///
  /// In en, this message translates to:
  /// **'Type your answer'**
  String get quizTypingLabelAi;

  /// No description provided for @quizTypingLabelDefault.
  ///
  /// In en, this message translates to:
  /// **'Type the answer'**
  String get quizTypingLabelDefault;

  /// No description provided for @quizTypingHelperAi.
  ///
  /// In en, this message translates to:
  /// **'AI will grade how accurate your meaning is.'**
  String get quizTypingHelperAi;

  /// No description provided for @quizTypingHelperDefault.
  ///
  /// In en, this message translates to:
  /// **'Case and trailing punctuation don\'t matter.'**
  String get quizTypingHelperDefault;

  /// No description provided for @quizTypingAiEvaluating.
  ///
  /// In en, this message translates to:
  /// **'AI is grading...'**
  String get quizTypingAiEvaluating;

  /// No description provided for @quizTypingSubmit.
  ///
  /// In en, this message translates to:
  /// **'Submit answer'**
  String get quizTypingSubmit;

  /// No description provided for @quizTypingEmptyAnswer.
  ///
  /// In en, this message translates to:
  /// **'Please enter an answer.'**
  String get quizTypingEmptyAnswer;

  /// No description provided for @quizTypingCorrect.
  ///
  /// In en, this message translates to:
  /// **'Correct'**
  String get quizTypingCorrect;

  /// No description provided for @quizTypingIncorrect.
  ///
  /// In en, this message translates to:
  /// **'Not quite right'**
  String get quizTypingIncorrect;

  /// No description provided for @quizTypingExpectedAnswer.
  ///
  /// In en, this message translates to:
  /// **'Answer: {answer}'**
  String quizTypingExpectedAnswer(String answer);

  /// No description provided for @quizTypingAiScore.
  ///
  /// In en, this message translates to:
  /// **'AI score: {score}%'**
  String quizTypingAiScore(int score);

  /// No description provided for @quizTypingAiSuggestion.
  ///
  /// In en, this message translates to:
  /// **'Suggestion: {suggestion}'**
  String quizTypingAiSuggestion(String suggestion);

  /// No description provided for @quizSessionSubmitError.
  ///
  /// In en, this message translates to:
  /// **'Could not submit the answer. Please try again.'**
  String get quizSessionSubmitError;

  /// No description provided for @quizSessionFinishError.
  ///
  /// In en, this message translates to:
  /// **'Could not finish the quiz. Please try again.'**
  String get quizSessionFinishError;

  /// No description provided for @quizResultHeadlineGreat.
  ///
  /// In en, this message translates to:
  /// **'Excellent!'**
  String get quizResultHeadlineGreat;

  /// No description provided for @quizResultHeadlineGood.
  ///
  /// In en, this message translates to:
  /// **'Well done!'**
  String get quizResultHeadlineGood;

  /// No description provided for @quizResultHeadlineTryAgain.
  ///
  /// In en, this message translates to:
  /// **'Keep practicing!'**
  String get quizResultHeadlineTryAgain;

  /// No description provided for @quizResultSummary.
  ///
  /// In en, this message translates to:
  /// **'{correct}/{total} correct · {duration}'**
  String quizResultSummary(int correct, int total, String duration);

  /// No description provided for @quizResultAccuracyLabel.
  ///
  /// In en, this message translates to:
  /// **'accuracy'**
  String get quizResultAccuracyLabel;

  /// No description provided for @quizResultCorrectLabel.
  ///
  /// In en, this message translates to:
  /// **'Correct'**
  String get quizResultCorrectLabel;

  /// No description provided for @quizResultWrongLabel.
  ///
  /// In en, this message translates to:
  /// **'Wrong'**
  String get quizResultWrongLabel;

  /// No description provided for @quizResultBestStreakLabel.
  ///
  /// In en, this message translates to:
  /// **'Best streak'**
  String get quizResultBestStreakLabel;

  /// No description provided for @quizResultListTitle.
  ///
  /// In en, this message translates to:
  /// **'RESULTS'**
  String get quizResultListTitle;

  /// No description provided for @quizResultYourAnswerLabel.
  ///
  /// In en, this message translates to:
  /// **'You answered'**
  String get quizResultYourAnswerLabel;

  /// No description provided for @quizResultNoAnswer.
  ///
  /// In en, this message translates to:
  /// **'No answer'**
  String get quizResultNoAnswer;

  /// No description provided for @quizResultAnswerLabel.
  ///
  /// In en, this message translates to:
  /// **'Answer'**
  String get quizResultAnswerLabel;

  /// No description provided for @quizResultReviewWrongButton.
  ///
  /// In en, this message translates to:
  /// **'Review wrong words'**
  String get quizResultReviewWrongButton;

  /// No description provided for @quizResultRetryButton.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get quizResultRetryButton;

  /// No description provided for @quizResultDoneButton.
  ///
  /// In en, this message translates to:
  /// **'Done'**
  String get quizResultDoneButton;

  /// No description provided for @quizResultLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load the quiz result.'**
  String get quizResultLoadError;

  /// No description provided for @quizResultRetryLoadButton.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get quizResultRetryLoadButton;

  /// No description provided for @quizResultInvalidSession.
  ///
  /// In en, this message translates to:
  /// **'Invalid quiz session code.'**
  String get quizResultInvalidSession;

  /// No description provided for @quizWrongWordsTitle.
  ///
  /// In en, this message translates to:
  /// **'Wrong words'**
  String get quizWrongWordsTitle;

  /// No description provided for @quizWrongWordsRetryButton.
  ///
  /// In en, this message translates to:
  /// **'Retest'**
  String get quizWrongWordsRetryButton;

  /// No description provided for @quizWrongWordsEmpty.
  ///
  /// In en, this message translates to:
  /// **'You don\'t have any words in your wrong list yet.'**
  String get quizWrongWordsEmpty;

  /// No description provided for @quizWrongWordsNoMeaning.
  ///
  /// In en, this message translates to:
  /// **'No meaning yet'**
  String get quizWrongWordsNoMeaning;

  /// No description provided for @quizWrongWordsStats.
  ///
  /// In en, this message translates to:
  /// **'Correct: {correct} · Wrong: {wrong}'**
  String quizWrongWordsStats(int correct, int wrong);

  /// No description provided for @quizWrongWordsMasteryLevel.
  ///
  /// In en, this message translates to:
  /// **'Lv.{level}'**
  String quizWrongWordsMasteryLevel(int level);

  /// No description provided for @quizWrongWordsLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load the wrong words list.'**
  String get quizWrongWordsLoadError;

  /// No description provided for @quizWrongWordsLoadMoreError.
  ///
  /// In en, this message translates to:
  /// **'Could not load more wrong words.'**
  String get quizWrongWordsLoadMoreError;

  /// No description provided for @quizWrongWordsRemoveError.
  ///
  /// In en, this message translates to:
  /// **'Could not remove the word from the wrong list.'**
  String get quizWrongWordsRemoveError;

  /// No description provided for @settingsSectionAppearance.
  ///
  /// In en, this message translates to:
  /// **'APPEARANCE'**
  String get settingsSectionAppearance;

  /// No description provided for @settingsDarkMode.
  ///
  /// In en, this message translates to:
  /// **'Dark mode'**
  String get settingsDarkMode;

  /// No description provided for @settingsDarkModeSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Switch to dark theme'**
  String get settingsDarkModeSubtitle;

  /// No description provided for @settingsFollowSystemTheme.
  ///
  /// In en, this message translates to:
  /// **'Follow system theme'**
  String get settingsFollowSystemTheme;

  /// No description provided for @settingsSectionLanguage.
  ///
  /// In en, this message translates to:
  /// **'LANGUAGE'**
  String get settingsSectionLanguage;

  /// No description provided for @settingsAppLanguage.
  ///
  /// In en, this message translates to:
  /// **'App language'**
  String get settingsAppLanguage;

  /// No description provided for @settingsSectionNotifications.
  ///
  /// In en, this message translates to:
  /// **'NOTIFICATIONS'**
  String get settingsSectionNotifications;

  /// No description provided for @settingsDailyReminder.
  ///
  /// In en, this message translates to:
  /// **'Daily reminder'**
  String get settingsDailyReminder;

  /// No description provided for @settingsDailyReminderSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Remind me to study each day'**
  String get settingsDailyReminderSubtitle;

  /// No description provided for @settingsStreakAlert.
  ///
  /// In en, this message translates to:
  /// **'Streak alert'**
  String get settingsStreakAlert;

  /// No description provided for @settingsStreakAlertSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Warn before streak breaks'**
  String get settingsStreakAlertSubtitle;

  /// No description provided for @settingsReviewDue.
  ///
  /// In en, this message translates to:
  /// **'Review due'**
  String get settingsReviewDue;

  /// No description provided for @settingsReviewDueSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Words ready for spaced review'**
  String get settingsReviewDueSubtitle;

  /// No description provided for @settingsSectionAudio.
  ///
  /// In en, this message translates to:
  /// **'AUDIO'**
  String get settingsSectionAudio;

  /// No description provided for @settingsAutoPlayPronunciation.
  ///
  /// In en, this message translates to:
  /// **'Auto-play pronunciation'**
  String get settingsAutoPlayPronunciation;

  /// No description provided for @settingsAutoPlayPronunciationSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Play when viewing a word'**
  String get settingsAutoPlayPronunciationSubtitle;

  /// No description provided for @settingsSoundEffects.
  ///
  /// In en, this message translates to:
  /// **'Sound effects'**
  String get settingsSoundEffects;

  /// No description provided for @settingsSoundEffectsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Quiz sounds and feedback'**
  String get settingsSoundEffectsSubtitle;

  /// No description provided for @settingsSectionAccount.
  ///
  /// In en, this message translates to:
  /// **'ACCOUNT'**
  String get settingsSectionAccount;

  /// No description provided for @settingsPrivacyPolicy.
  ///
  /// In en, this message translates to:
  /// **'Privacy policy'**
  String get settingsPrivacyPolicy;

  /// No description provided for @settingsDeleteAccount.
  ///
  /// In en, this message translates to:
  /// **'Delete account'**
  String get settingsDeleteAccount;

  /// No description provided for @settingsVersionLabel.
  ///
  /// In en, this message translates to:
  /// **'VocaNova v{version}'**
  String settingsVersionLabel(String version);

  /// No description provided for @settingsPrivacyPolicyBody.
  ///
  /// In en, this message translates to:
  /// **'VocaNova stores only the account and learning data needed to provide vocabulary practice, progress tracking, and synchronization. Your credentials are protected and are never displayed in the app.'**
  String get settingsPrivacyPolicyBody;

  /// No description provided for @settingsBackToProfile.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get settingsBackToProfile;

  /// No description provided for @settingsTitle.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get settingsTitle;

  /// No description provided for @settingsDeleteAccountDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete account?'**
  String get settingsDeleteAccountDialogTitle;

  /// No description provided for @settingsDeleteAccountDialogBody.
  ///
  /// In en, this message translates to:
  /// **'This action permanently removes your profile and learning data. It cannot be undone.'**
  String get settingsDeleteAccountDialogBody;

  /// No description provided for @settingsCancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get settingsCancel;

  /// No description provided for @settingsContinue.
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get settingsContinue;

  /// No description provided for @settingsDeleteAccountFailed.
  ///
  /// In en, this message translates to:
  /// **'Unable to delete the account. Please try again.'**
  String get settingsDeleteAccountFailed;

  /// No description provided for @settingsDone.
  ///
  /// In en, this message translates to:
  /// **'Done'**
  String get settingsDone;

  /// No description provided for @profileSectionLearning.
  ///
  /// In en, this message translates to:
  /// **'LEARNING'**
  String get profileSectionLearning;

  /// No description provided for @profileMyVocabulary.
  ///
  /// In en, this message translates to:
  /// **'My vocabulary'**
  String get profileMyVocabulary;

  /// No description provided for @profileMyVocabularySubtitle.
  ///
  /// In en, this message translates to:
  /// **'248 words collected'**
  String get profileMyVocabularySubtitle;

  /// No description provided for @profileStatistics.
  ///
  /// In en, this message translates to:
  /// **'Statistics'**
  String get profileStatistics;

  /// No description provided for @profileStatisticsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Progress & analytics'**
  String get profileStatisticsSubtitle;

  /// No description provided for @profileTestHistory.
  ///
  /// In en, this message translates to:
  /// **'Test history'**
  String get profileTestHistory;

  /// No description provided for @profileTestHistorySubtitle.
  ///
  /// In en, this message translates to:
  /// **'Past practice sessions'**
  String get profileTestHistorySubtitle;

  /// No description provided for @profileLearningGoals.
  ///
  /// In en, this message translates to:
  /// **'Learning goals'**
  String get profileLearningGoals;

  /// No description provided for @profileLearningGoalsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'B2 → C1 target'**
  String get profileLearningGoalsSubtitle;

  /// No description provided for @profileSectionAccount.
  ///
  /// In en, this message translates to:
  /// **'ACCOUNT'**
  String get profileSectionAccount;

  /// No description provided for @profilePersonalInformation.
  ///
  /// In en, this message translates to:
  /// **'Personal information'**
  String get profilePersonalInformation;

  /// No description provided for @profilePersonalInformationSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Name, avatar, phone'**
  String get profilePersonalInformationSubtitle;

  /// No description provided for @profileNotifications.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get profileNotifications;

  /// No description provided for @profileDailyRemindersOn.
  ///
  /// In en, this message translates to:
  /// **'Daily reminders on'**
  String get profileDailyRemindersOn;

  /// No description provided for @profileDailyRemindersOff.
  ///
  /// In en, this message translates to:
  /// **'Daily reminders off'**
  String get profileDailyRemindersOff;

  /// No description provided for @profileLanguage.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get profileLanguage;

  /// No description provided for @profileLanguageEnglish.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get profileLanguageEnglish;

  /// No description provided for @profileLanguageVietnamese.
  ///
  /// In en, this message translates to:
  /// **'Vietnamese'**
  String get profileLanguageVietnamese;

  /// No description provided for @profileTheme.
  ///
  /// In en, this message translates to:
  /// **'Theme'**
  String get profileTheme;

  /// No description provided for @profileThemeDark.
  ///
  /// In en, this message translates to:
  /// **'Dark mode'**
  String get profileThemeDark;

  /// No description provided for @profileThemeLight.
  ///
  /// In en, this message translates to:
  /// **'Light mode'**
  String get profileThemeLight;

  /// No description provided for @profileThemeSystem.
  ///
  /// In en, this message translates to:
  /// **'System default'**
  String get profileThemeSystem;

  /// No description provided for @profileSectionApp.
  ///
  /// In en, this message translates to:
  /// **'APP'**
  String get profileSectionApp;

  /// No description provided for @profileSettingsMenuTitle.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get profileSettingsMenuTitle;

  /// No description provided for @profileSettingsMenuSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Audio, storage, sync'**
  String get profileSettingsMenuSubtitle;

  /// No description provided for @profilePrivacyData.
  ///
  /// In en, this message translates to:
  /// **'Privacy & data'**
  String get profilePrivacyData;

  /// No description provided for @profilePrivacyDataSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Manage your data'**
  String get profilePrivacyDataSubtitle;

  /// No description provided for @profilePrivacyDataBody.
  ///
  /// In en, this message translates to:
  /// **'VocaNova stores your profile and learning progress so your vocabulary can stay synchronized across sessions.'**
  String get profilePrivacyDataBody;

  /// No description provided for @profileHelpFeedback.
  ///
  /// In en, this message translates to:
  /// **'Help & feedback'**
  String get profileHelpFeedback;

  /// No description provided for @profileHelpFeedbackSubtitle.
  ///
  /// In en, this message translates to:
  /// **'FAQs and support'**
  String get profileHelpFeedbackSubtitle;

  /// No description provided for @profileHelpFeedbackBody.
  ///
  /// In en, this message translates to:
  /// **'Need a hand? Share the issue, the screen you were using, and the steps that caused it with the VocaNova support team.'**
  String get profileHelpFeedbackBody;

  /// No description provided for @profileSignOut.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get profileSignOut;

  /// No description provided for @profileVersionLabel.
  ///
  /// In en, this message translates to:
  /// **'VocaNova v1.0.0 · SEP490_19'**
  String get profileVersionLabel;

  /// No description provided for @profileUploadAvatarFailed.
  ///
  /// In en, this message translates to:
  /// **'Unable to upload avatar.'**
  String get profileUploadAvatarFailed;

  /// No description provided for @profileUpdateSuccess.
  ///
  /// In en, this message translates to:
  /// **'Profile updated successfully.'**
  String get profileUpdateSuccess;

  /// No description provided for @profileUpdateFailed.
  ///
  /// In en, this message translates to:
  /// **'Unable to update profile.'**
  String get profileUpdateFailed;

  /// No description provided for @profilePasswordChangeSuccess.
  ///
  /// In en, this message translates to:
  /// **'Password changed successfully.'**
  String get profilePasswordChangeSuccess;

  /// No description provided for @profilePasswordChangeFailed.
  ///
  /// In en, this message translates to:
  /// **'Unable to change password.'**
  String get profilePasswordChangeFailed;

  /// No description provided for @profileDone.
  ///
  /// In en, this message translates to:
  /// **'Done'**
  String get profileDone;

  /// No description provided for @profileSignOutConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign out?'**
  String get profileSignOutConfirmTitle;

  /// No description provided for @profileSignOutConfirmBody.
  ///
  /// In en, this message translates to:
  /// **'You will need to sign in again to keep learning.'**
  String get profileSignOutConfirmBody;

  /// No description provided for @profileCancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get profileCancel;

  /// No description provided for @profilePhoneNotLinked.
  ///
  /// In en, this message translates to:
  /// **'Phone not linked'**
  String get profilePhoneNotLinked;

  /// No description provided for @profileLevelB2.
  ///
  /// In en, this message translates to:
  /// **'B2 level'**
  String get profileLevelB2;

  /// No description provided for @profileStreakLabel.
  ///
  /// In en, this message translates to:
  /// **'{days}-day streak'**
  String profileStreakLabel(int days);

  /// No description provided for @profileEditAction.
  ///
  /// In en, this message translates to:
  /// **'Edit'**
  String get profileEditAction;

  /// No description provided for @profileStatWords.
  ///
  /// In en, this message translates to:
  /// **'Words'**
  String get profileStatWords;

  /// No description provided for @profileStatAccuracy.
  ///
  /// In en, this message translates to:
  /// **'Accuracy'**
  String get profileStatAccuracy;

  /// No description provided for @profileStatStreak.
  ///
  /// In en, this message translates to:
  /// **'Streak'**
  String get profileStatStreak;

  /// No description provided for @profileStatBadges.
  ///
  /// In en, this message translates to:
  /// **'Badges'**
  String get profileStatBadges;

  /// No description provided for @profileEditSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Update your profile details.'**
  String get profileEditSubtitle;

  /// No description provided for @profileFieldPicture.
  ///
  /// In en, this message translates to:
  /// **'Profile picture'**
  String get profileFieldPicture;

  /// No description provided for @profileChooseAvatar.
  ///
  /// In en, this message translates to:
  /// **'Choose an avatar'**
  String get profileChooseAvatar;

  /// No description provided for @profileFieldFullName.
  ///
  /// In en, this message translates to:
  /// **'Full name'**
  String get profileFieldFullName;

  /// No description provided for @profileNameHint.
  ///
  /// In en, this message translates to:
  /// **'Nguyen Van An'**
  String get profileNameHint;

  /// No description provided for @profileNameTooShort.
  ///
  /// In en, this message translates to:
  /// **'Name must contain at least 2 characters'**
  String get profileNameTooShort;

  /// No description provided for @profileFieldPhoneNumber.
  ///
  /// In en, this message translates to:
  /// **'Phone number'**
  String get profileFieldPhoneNumber;

  /// No description provided for @profilePhoneNotLinkedShort.
  ///
  /// In en, this message translates to:
  /// **'Not linked'**
  String get profilePhoneNotLinkedShort;

  /// No description provided for @profileChangePassword.
  ///
  /// In en, this message translates to:
  /// **'Change password'**
  String get profileChangePassword;

  /// No description provided for @profileSaveChanges.
  ///
  /// In en, this message translates to:
  /// **'Save changes'**
  String get profileSaveChanges;

  /// No description provided for @profileAvatarOpening.
  ///
  /// In en, this message translates to:
  /// **'Opening...'**
  String get profileAvatarOpening;

  /// No description provided for @profileChooseFromDevice.
  ///
  /// In en, this message translates to:
  /// **'Choose from device'**
  String get profileChooseFromDevice;

  /// No description provided for @profileAvatarHint.
  ///
  /// In en, this message translates to:
  /// **'JPG, PNG or WebP · Max 5MB'**
  String get profileAvatarHint;

  /// No description provided for @profileAvatarTooLarge.
  ///
  /// In en, this message translates to:
  /// **'Avatar must be 5MB or smaller.'**
  String get profileAvatarTooLarge;

  /// No description provided for @profilePhotoLibraryError.
  ///
  /// In en, this message translates to:
  /// **'Unable to open the photo library.'**
  String get profilePhotoLibraryError;

  /// No description provided for @profileChangePasswordSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Use at least 8 characters with upper, lower and a number.'**
  String get profileChangePasswordSubtitle;

  /// No description provided for @profileFieldCurrentPassword.
  ///
  /// In en, this message translates to:
  /// **'Current password'**
  String get profileFieldCurrentPassword;

  /// No description provided for @profileCurrentPasswordHint.
  ///
  /// In en, this message translates to:
  /// **'Enter current password'**
  String get profileCurrentPasswordHint;

  /// No description provided for @profileFieldNewPassword.
  ///
  /// In en, this message translates to:
  /// **'New password'**
  String get profileFieldNewPassword;

  /// No description provided for @profileNewPasswordHint.
  ///
  /// In en, this message translates to:
  /// **'At least 8 characters'**
  String get profileNewPasswordHint;

  /// No description provided for @profileFieldConfirmPassword.
  ///
  /// In en, this message translates to:
  /// **'Confirm new password'**
  String get profileFieldConfirmPassword;

  /// No description provided for @profileConfirmPasswordHint.
  ///
  /// In en, this message translates to:
  /// **'Repeat your password'**
  String get profileConfirmPasswordHint;

  /// No description provided for @profileUpdatePassword.
  ///
  /// In en, this message translates to:
  /// **'Update password'**
  String get profileUpdatePassword;

  /// No description provided for @profileClose.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get profileClose;

  /// No description provided for @profileHidePassword.
  ///
  /// In en, this message translates to:
  /// **'Hide password'**
  String get profileHidePassword;

  /// No description provided for @profileShowPassword.
  ///
  /// In en, this message translates to:
  /// **'Show password'**
  String get profileShowPassword;

  /// No description provided for @profileTryAgain.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get profileTryAgain;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'vi'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'vi':
      return AppLocalizationsVi();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
