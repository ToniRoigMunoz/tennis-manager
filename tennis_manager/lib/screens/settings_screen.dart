import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../config.dart';
import '../services/api_service.dart';
import '../providers/tournament_provider.dart';
import '../providers/league_provider.dart';
import '../providers/tournament_flow_provider.dart';
import 'widgets/profile_header_card.dart';
import 'widgets/settings_section.dart';
import 'widgets/settings_tile.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  bool _matchReminders = true;
  bool _tournamentAlerts = true;
  bool _weeklySummary = false;
  bool _soundEffects = true;
  bool _haptics = true;
  final bool _darkMode = false;

  bool _advancingDay = false;

  Future<void> _advanceDay() async {
    if (_advancingDay) return;
    setState(() => _advancingDay = true);

    try {
      final result = await ApiService.advanceDay(Config.demoUserId);

      // Refrescar los datos que cambian al avanzar el día
      ref.invalidate(tournamentProvider); // día de temporada
      ref.invalidate(leagueProvider); // clasificación (bots puntuaron)
      ref.invalidate(tournamentFlowProvider); // nuevo torneo del día

      if (mounted) _showResultDialog(result);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('Error al avanzar el día: $e')));
      }
    } finally {
      if (mounted) setState(() => _advancingDay = false);
    }
  }

  void _showResultDialog(Map<String, dynamic> r) {
    final prevDay = r['previousDay'];
    final newDay = r['newDay'];
    final totalDays = r['totalDays'];
    final botsRewarded = r['botsRewarded'] ?? 0;
    final elapsedMs = r['elapsedMs'] ?? 0;
    final seasonFinished = r['seasonFinished'] ?? false;
    final note = r['distributionNote'];

    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.wb_sunny_rounded, color: Colors.orange, size: 20),
            SizedBox(width: 8),
            Text('Día avanzado'),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _DialogRow(label: 'Día', value: '$prevDay → $newDay de $totalDays'),
            _DialogRow(label: 'Bots recompensados', value: '$botsRewarded'),
            _DialogRow(label: 'Tiempo de proceso', value: '$elapsedMs ms'),
            if (seasonFinished)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text(
                  '⚠ Última jornada de la temporada',
                  style: TextStyle(fontSize: 12, color: Colors.deepOrange),
                ),
              ),
            if (note != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  note,
                  style: const TextStyle(
                    fontSize: 12,
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Entendido'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
      children: [
        ProfileHeaderCard(
          name: 'Toni Roig',
          subtitle: 'Jugador desde junio de 2026 · Liga Élite',
          onEditTap: () {},
        ),
        const SizedBox(height: 24),
        const SettingsSection(
          title: 'Cuenta',
          children: [
            SettingsTile(
              icon: Icons.email_outlined,
              title: 'Correo electrónico',
              trailingText: 'toni@email.com',
            ),
            SettingsTile(
              icon: Icons.lock_outline_rounded,
              title: 'Cambiar contraseña',
            ),
            SettingsTile(
              icon: Icons.link_rounded,
              title: 'Vincular cuenta de Google',
              badgeText: 'Próximamente',
              showChevron: false,
            ),
          ],
        ),
        const SizedBox(height: 20),
        SettingsSection(
          title: 'Notificaciones',
          children: [
            SettingsTile(
              icon: Icons.notifications_outlined,
              title: 'Recordatorio de partidos',
              showChevron: false,
              trailingWidget: Switch(
                value: _matchReminders,
                onChanged: (v) => setState(() => _matchReminders = v),
              ),
            ),
            SettingsTile(
              icon: Icons.emoji_events_outlined,
              title: 'Alertas de torneos',
              showChevron: false,
              trailingWidget: Switch(
                value: _tournamentAlerts,
                onChanged: (v) => setState(() => _tournamentAlerts = v),
              ),
            ),
            SettingsTile(
              icon: Icons.summarize_outlined,
              title: 'Resumen semanal',
              showChevron: false,
              trailingWidget: Switch(
                value: _weeklySummary,
                onChanged: (v) => setState(() => _weeklySummary = v),
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),
        SettingsSection(
          title: 'Preferencias',
          children: [
            SettingsTile(
              icon: Icons.volume_up_outlined,
              title: 'Efectos de sonido',
              showChevron: false,
              trailingWidget: Switch(
                value: _soundEffects,
                onChanged: (v) => setState(() => _soundEffects = v),
              ),
            ),
            SettingsTile(
              icon: Icons.vibration_rounded,
              title: 'Vibración',
              showChevron: false,
              trailingWidget: Switch(
                value: _haptics,
                onChanged: (v) => setState(() => _haptics = v),
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),
        // ── SECCIÓN DE DESARROLLO (solo para pruebas, no para producción) ──
        SettingsSection(
          title: 'Desarrollo',
          children: [
            SettingsTile(
              icon: _advancingDay
                  ? Icons.hourglass_top_rounded
                  : Icons.skip_next_rounded,
              iconColor: Colors.deepOrange,
              title: _advancingDay ? 'Avanzando día…' : 'Avanzar día',
              badgeText: 'DEV',
              showChevron: false,
              onTap: _advancingDay ? null : _advanceDay,
            ),
          ],
        ),
        const SizedBox(height: 12),
        Center(
          child: Text(
            'La sección de desarrollo no estará en la versión final',
            style: TextStyle(
              fontSize: 11,
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
        ),
      ],
    );
  }
}

class _DialogRow extends StatelessWidget {
  final String label;
  final String value;
  const _DialogRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontSize: 13)),
          Text(
            value,
            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }
}
