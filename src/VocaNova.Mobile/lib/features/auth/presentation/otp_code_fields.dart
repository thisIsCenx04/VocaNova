import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

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
    return Row(
      children: List.generate(length, (index) {
        return Expanded(
          child: Padding(
            padding: EdgeInsets.only(right: index == length - 1 ? 0 : 8),
            child: TextField(
              key: Key('otp-digit-$index'),
              controller: _controllers[index],
              focusNode: _focusNodes[index],
              enabled: widget.enabled,
              autofocus: index == 0,
              keyboardType: TextInputType.number,
              textAlign: TextAlign.center,
              maxLength: 1,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              decoration: const InputDecoration(counterText: ''),
              onChanged: (value) => _onChanged(index, value),
            ),
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
