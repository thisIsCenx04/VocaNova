import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';

class OtpCodeFields extends StatefulWidget {
  const OtpCodeFields({
    required this.onCompleted,
    this.enabled = true,
    super.key,
  });

  final ValueChanged<String> onCompleted;
  final bool enabled;

  @override
  State<OtpCodeFields> createState() => OtpCodeFieldsState();
}

class OtpCodeFieldsState extends State<OtpCodeFields> {
  static const length = 6;
  late final List<TextEditingController> _controllers;
  late final List<FocusNode> _focusNodes;

  String get code => _controllers.map((controller) => controller.text).join();

  @override
  void initState() {
    super.initState();
    _controllers = List.generate(length, (_) => TextEditingController());
    _focusNodes = List.generate(length, (_) => FocusNode());
  }

  @override
  void dispose() {
    for (final controller in _controllers) {
      controller.dispose();
    }
    for (final focusNode in _focusNodes) {
      focusNode.dispose();
    }
    super.dispose();
  }

  void clear() {
    for (final controller in _controllers) {
      controller.clear();
    }
    _focusNodes.first.requestFocus();
  }

  @override
  Widget build(BuildContext context) {
    return Wrap(
      alignment: WrapAlignment.center,
      spacing: 10,
      runSpacing: 10,
      children: List.generate(length, (index) {
        return SizedBox(
          width: 40,
          height: 48,
          child: TextField(
            key: Key('otp-digit-$index'),
            controller: _controllers[index],
            focusNode: _focusNodes[index],
            enabled: widget.enabled,
            autofocus: index == 0,
            keyboardType: TextInputType.number,
            textAlign: TextAlign.center,
            maxLength: 1,
            style: AppTextStyles.body.copyWith(
              fontSize: 18,
              fontWeight: FontWeight.w600,
            ),
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            decoration: const InputDecoration(
              counterText: '',
              contentPadding: EdgeInsets.zero,
            ),
            onChanged: (value) => _onChanged(index, value),
          ),
        );
      }),
    );
  }

  void _onChanged(int index, String value) {
    if (value.isEmpty) {
      if (index > 0) {
        _focusNodes[index - 1].requestFocus();
      }
      return;
    }
    if (index < length - 1) {
      _focusNodes[index + 1].requestFocus();
    } else {
      _focusNodes[index].unfocus();
    }
    if (code.length == length) {
      widget.onCompleted(code);
    }
  }
}
