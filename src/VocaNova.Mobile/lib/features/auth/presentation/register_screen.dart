import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/models/onboarding_catalog.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_request_error.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  static const _minAge = 5;
  static const _maxAge = 120;

  final _formKey = GlobalKey<FormState>();
  final _displayNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _obscurePassword = true;
  bool _obscureConfirmation = true;
  bool _isLoading = false;

  LearningProfileOptions _options = LearningProfileOptions.empty;
  DateTime? _dateOfBirth;
  int? _regionId;
  int? _occupationId;
  int? _educationLevelId;

  @override
  void initState() {
    super.initState();
    _loadOptions();
  }

  @override
  void dispose() {
    _displayNameController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  /// The catalog is a nice-to-have: if it cannot be loaded the optional dropdowns simply
  /// stay hidden and sign-up still works.
  Future<void> _loadOptions() async {
    try {
      final options = await ref
          .read(authApiServiceProvider)
          .getLearningProfileOptions();
      if (mounted) {
        setState(() => _options = options);
      }
    } catch (_) {
      // Ignored on purpose — see above.
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return AuthFormScaffold(
      title: l10n.authCreateAccountTitle,
      subtitle: l10n.authStartLearningSubtitle,
      showBackButton: true,
      onBack: () => context.go(AppRoutes.login),
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              key: const Key('register-display-name'),
              controller: _displayNameController,
              enabled: !_isLoading,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: l10n.authFullNameLabel,
                requiredField: true,
                hint: 'Nguyen Van An',
              ),
              validator: AuthValidators.displayName,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-phone'),
              controller: _phoneController,
              enabled: !_isLoading,
              keyboardType: TextInputType.phone,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: l10n.authPhoneNumberLabel,
                requiredField: true,
                hint: '+84 90 000 0000',
              ),
              validator: AuthValidators.phone,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-password'),
              controller: _passwordController,
              enabled: !_isLoading,
              obscureText: _obscurePassword,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: l10n.authPasswordLabel,
                requiredField: true,
                hint: l10n.authPasswordHintMinChars,
                suffixIcon: _visibilityButton(
                  obscure: _obscurePassword,
                  onPressed: () {
                    setState(() => _obscurePassword = !_obscurePassword);
                  },
                ),
              ),
              validator: AuthValidators.strongPassword,
              onChanged: (_) {
                if (_confirmPasswordController.text.isNotEmpty) {
                  _formKey.currentState?.validate();
                }
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-confirm-password'),
              controller: _confirmPasswordController,
              enabled: !_isLoading,
              obscureText: _obscureConfirmation,
              textInputAction: TextInputAction.done,
              decoration: authInputDecoration(
                label: l10n.authConfirmPasswordLabel,
                requiredField: true,
                hint: l10n.authRepeatPasswordHint,
                suffixIcon: _visibilityButton(
                  obscure: _obscureConfirmation,
                  onPressed: () {
                    setState(
                      () => _obscureConfirmation = !_obscureConfirmation,
                    );
                  },
                ),
              ),
              validator: (value) => AuthValidators.confirmPassword(
                value,
                _passwordController.text,
              ),
              onFieldSubmitted: (_) => _submit(),
            ),
            const SizedBox(height: 24),
            _learningProfileSection(),
            AuthPrimaryButton(
              buttonKey: const Key('register-submit'),
              onPressed: _isLoading ? null : _submit,
              child: _isLoading
                  ? authLoadingIndicator()
                  : Text(l10n.authCreateAccountTitle),
            ),
            const SizedBox(height: 16),
            const AuthDivider(),
            const SizedBox(height: 16),
            GoogleAuthButton(
              onPressed: _isLoading
                  ? null
                  : () => ref.read(authProvider.notifier).signInWithGoogle(),
            ),
            const SizedBox(height: 12),
            AuthInlineLink(
              text: l10n.authAlreadyHaveAccountPrefix,
              actionText: l10n.authSignInTitle,
              onPressed: _isLoading ? null : () => context.go(AppRoutes.login),
            ),
          ],
        ),
      ),
    );
  }

  /// Optional questions that seed the learning profile. They are deliberately collapsed
  /// into a clearly-labelled optional block so the required sign-up fields stay prominent.
  Widget _learningProfileSection() {
    final hasLookups =
        _options.regions.isNotEmpty ||
        _options.occupations.isNotEmpty ||
        _options.educationLevels.isNotEmpty;
    final l10n = AppLocalizations.of(context)!;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _dateOfBirthField(),
        if (_options.regions.isNotEmpty) ...[
          const SizedBox(height: 16),
          _lookupDropdown(
            fieldKey: const Key('register-region'),
            label: l10n.authRegionLabel,
            options: _options.regions,
            value: _regionId,
            onChanged: (id) => setState(() => _regionId = id),
          ),
        ],
        if (_options.occupations.isNotEmpty) ...[
          const SizedBox(height: 16),
          _lookupDropdown(
            fieldKey: const Key('register-occupation'),
            label: l10n.authOccupationLabel,
            options: _options.occupations,
            value: _occupationId,
            onChanged: (id) => setState(() => _occupationId = id),
          ),
        ],
        if (_options.educationLevels.isNotEmpty) ...[
          const SizedBox(height: 16),
          _lookupDropdown(
            fieldKey: const Key('register-education-level'),
            label: l10n.authEducationLevelLabel,
            options: _options.educationLevels,
            value: _educationLevelId,
            onChanged: (id) => setState(() => _educationLevelId = id),
          ),
        ],
        SizedBox(height: hasLookups ? 24 : 16),
      ],
    );
  }

  Widget _dateOfBirthField() {
    final dateOfBirth = _dateOfBirth;
    final l10n = AppLocalizations.of(context)!;
    return InkWell(
      key: const Key('register-date-of-birth'),
      onTap: _isLoading ? null : _pickDateOfBirth,
      child: InputDecorator(
        decoration: authInputDecoration(
          label: l10n.authDateOfBirthLabel,
          hint: 'dd/mm/yyyy',
          suffixIcon: const Icon(Icons.calendar_today_outlined),
        ),
        child: Text(
          dateOfBirth == null
              ? l10n.authSelectDateOfBirth
              : _formatDate(dateOfBirth),
          style: dateOfBirth == null
              ? Theme.of(context).inputDecorationTheme.hintStyle
              : null,
        ),
      ),
    );
  }

  Widget _lookupDropdown({
    required Key fieldKey,
    required String label,
    required List<OnboardingOption> options,
    required int? value,
    required ValueChanged<int?> onChanged,
  }) {
    return DropdownButtonFormField<int>(
      key: fieldKey,
      initialValue: value,
      decoration: authInputDecoration(label: label),
      isExpanded: true,
      items: [
        for (final option in options)
          DropdownMenuItem(value: option.id, child: Text(option.label)),
      ],
      onChanged: _isLoading ? null : onChanged,
    );
  }

  Future<void> _pickDateOfBirth() async {
    final today = DateTime.now();
    final selected = await showDatePicker(
      context: context,
      initialDate:
          _dateOfBirth ?? DateTime(today.year - 18, today.month, today.day),
      firstDate: DateTime(today.year - _maxAge, today.month, today.day),
      lastDate: DateTime(today.year - _minAge, today.month, today.day),
      helpText: AppLocalizations.of(context)!.authSelectDateOfBirth,
    );
    if (selected != null && mounted) {
      setState(() => _dateOfBirth = selected);
    }
  }

  static String _formatDate(DateTime date) {
    final day = date.day.toString().padLeft(2, '0');
    final month = date.month.toString().padLeft(2, '0');
    return '$day/$month/${date.year}';
  }

  Widget _visibilityButton({
    required bool obscure,
    required VoidCallback onPressed,
  }) {
    final l10n = AppLocalizations.of(context)!;
    return IconButton(
      tooltip: obscure ? l10n.authShowPassword : l10n.authHidePassword,
      onPressed: onPressed,
      icon: Icon(
        obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    final phone = _phoneController.text.trim();
    final payload = RegisterOtpPayload(
      displayName: _displayNameController.text.trim(),
      password: _passwordController.text,
      dateOfBirth: _dateOfBirth,
      regionId: _regionId,
      occupationId: _occupationId,
      educationLevelId: _educationLevelId,
    );

    setState(() => _isLoading = true);
    try {
      await ref
          .read(authApiServiceProvider)
          .sendOtp(phone: phone, purpose: 'register');
      if (!mounted) {
        return;
      }
      context.go(AppRoutes.otpFor(phone, purpose: 'register'), extra: payload);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(authRequestError(error))));
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }
}
