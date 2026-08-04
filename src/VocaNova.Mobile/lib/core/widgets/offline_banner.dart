import 'package:flutter/material.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class OfflineBanner extends StatelessWidget {
  const OfflineBanner({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('global-offline-banner'),
      width: double.infinity,
      color: Colors.amber.shade700,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Text(
        AppLocalizations.of(context)!.commonOfflineBanner,
        textAlign: TextAlign.center,
        style: const TextStyle(color: Colors.black, fontWeight: FontWeight.w700),
      ),
    );
  }
}
