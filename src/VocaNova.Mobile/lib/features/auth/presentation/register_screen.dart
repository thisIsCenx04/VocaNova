import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/onboarding_catalog.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_request_error.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_screen.dart';

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
          .read(authRepositoryProvider)
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
    return AuthFormScaffold(
      title: 'Create account',
      subtitle: 'Start learning today',
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
                label: 'Full name',
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
                label: 'Phone number',
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
                label: 'Password',
                hint: 'At least 8 characters',
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
                label: 'Confirm password',
                hint: 'Repeat your password',
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
                  : const Text('Create account'),
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
              text: 'Already have an account? ',
              actionText: 'Sign in',
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

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Cá nhân hóa gợi ý học (không bắt buộc)',
          style: Theme.of(context).textTheme.titleSmall,
        ),
        const SizedBox(height: 4),
        Text(
          'Giúp VocaNova gợi ý chủ đề và từ vựng phù hợp ngay từ đầu.',
          style: Theme.of(context).textTheme.bodySmall,
        ),
        const SizedBox(height: 16),
        _dateOfBirthField(),
        if (_options.regions.isNotEmpty) ...[
          const SizedBox(height: 16),
          _lookupDropdown(
            fieldKey: const Key('register-region'),
            label: 'Khu vực',
            options: _options.regions,
            value: _regionId,
            onChanged: (id) => setState(() => _regionId = id),
          ),
        ],
        if (_options.occupations.isNotEmpty) ...[
          const SizedBox(height: 16),
          _lookupDropdown(
            fieldKey: const Key('register-occupation'),
            label: 'Nghề nghiệp',
            options: _options.occupations,
            value: _occupationId,
            onChanged: (id) => setState(() => _occupationId = id),
          ),
        ],
        if (_options.educationLevels.isNotEmpty) ...[
          const SizedBox(height: 16),
          _lookupDropdown(
            fieldKey: const Key('register-education-level'),
            label: 'Trình độ học vấn',
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
    return InkWell(
      key: const Key('register-date-of-birth'),
      onTap: _isLoading ? null : _pickDateOfBirth,
      child: InputDecorator(
        decoration: authInputDecoration(
          label: 'Ngày sinh',
          hint: 'dd/mm/yyyy',
          suffixIcon: const Icon(Icons.calendar_today_outlined),
        ),
        child: Text(
          dateOfBirth == null ? 'Chọn ngày sinh' : _formatDate(dateOfBirth),
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
      initialDate: _dateOfBirth ?? DateTime(today.year - 18, today.month, today.day),
      firstDate: DateTime(today.year - _maxAge, today.month, today.day),
      lastDate: DateTime(today.year - _minAge, today.month, today.day),
      helpText: 'Chọn ngày sinh',
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
    return IconButton(
      tooltip: obscure ? 'Show password' : 'Hide password',
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
          .read(authRepositoryProvider)
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
