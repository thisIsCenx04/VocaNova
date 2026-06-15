import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';

final connectivityServiceProvider = Provider<ConnectivityService>(
  (ref) => ConnectivityService(),
);

final connectivityProvider = StreamProvider<bool>((ref) async* {
  final service = ref.watch(connectivityServiceProvider);
  yield await service.isOnline;
  yield* service.statusStream;
});
