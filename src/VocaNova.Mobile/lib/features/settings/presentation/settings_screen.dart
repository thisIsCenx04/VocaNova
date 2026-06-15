import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';

const appVersion = '1.0.0+1';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: AppSettingsNotifier.instance,
      builder: (context, _) {
        final settings = AppSettingsNotifier.instance.state;
        return Scaffold(
          appBar: AppBar(title: const Text('Cài đặt')),
          body: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text('Ứng dụng', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 8),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      leading: const Icon(Icons.language),
                      title: const Text('Ngôn ngữ'),
                      trailing: DropdownButton<String>(
                        key: const Key('language-dropdown'),
                        value: settings.locale.languageCode,
                        items: const [
                          DropdownMenuItem(
                            value: 'vi',
                            child: Text('Tiếng Việt'),
                          ),
                          DropdownMenuItem(value: 'en', child: Text('English')),
                        ],
                        onChanged: (value) {
                          if (value != null) {
                            AppSettingsNotifier.instance.setLocale(value);
                          }
                        },
                      ),
                    ),
                    const Divider(height: 1),
                    SwitchListTile(
                      key: const Key('theme-switch'),
                      secondary: const Icon(Icons.dark_mode_outlined),
                      title: const Text('Giao diện tối'),
                      value: settings.themeMode == ThemeMode.dark,
                      onChanged: (dark) {
                        AppSettingsNotifier.instance.setThemeMode(
                          dark ? ThemeMode.dark : ThemeMode.light,
                        );
                      },
                    ),
                    const Divider(height: 1),
                    const SwitchListTile(
                      secondary: Icon(Icons.notifications_outlined),
                      title: Text('Thông báo'),
                      subtitle: Text('Tính năng đang được phát triển'),
                      value: false,
                      onChanged: null,
                    ),
                    const Divider(height: 1),
                    ListTile(
                      leading: const Icon(Icons.info_outline),
                      title: const Text('Phiên bản ứng dụng'),
                      trailing: const Text(appVersion),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
