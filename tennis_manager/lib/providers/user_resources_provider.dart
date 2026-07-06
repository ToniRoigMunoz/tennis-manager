import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'dashboard_provider.dart';

// Provider de conveniencia que lee recursos del dashboard.
// Las mutaciones (spendMoney, etc.) se hacen directamente sobre dashboardProvider.
final userResourcesProvider = Provider((ref) {
  final dashboard = ref.watch(dashboardProvider);
  return (
    money: dashboard.valueOrNull?.money ?? 0,
    rests: dashboard.valueOrNull?.rests ?? 0,
  );
});
