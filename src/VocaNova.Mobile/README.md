# VocaNova Mobile

Flutter client for the VocaNova vocabulary learning system.

## Run Locally

```powershell
flutter pub get
flutter run
```

The default API URL is `http://10.0.2.2:5013` for an Android emulator. Override
it for another device or environment:

```powershell
flutter run --dart-define=API_BASE_URL=http://192.168.1.10:5013
```

## Verify

```powershell
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
```
