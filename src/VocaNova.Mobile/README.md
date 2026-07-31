# VocaNova Mobile

Flutter client for the VocaNova vocabulary learning system.

## Run Locally

```powershell
flutter pub get
flutter run
```

The default API URL is `http://10.0.3.2:5013` for Genymotion. For the Android
Studio emulator, override it with `http://10.0.2.2:5013`. For a physical
device, use the development machine's LAN address:
it for another device or environment:

```powershell
flutter run --dart-define=API_BASE_URL=http://192.168.1.10:5013
```

Google sign-in needs the web OAuth client ID used by the API to verify ID
tokens:

```powershell
flutter run --dart-define=GOOGLE_SERVER_CLIENT_ID=your-web-client-id
```

## Verify

```powershell
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
```
