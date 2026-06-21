import 'package:flutter/material.dart';
import 'widgets/profile_header_card.dart';
import 'widgets/settings_section.dart';
import 'widgets/settings_tile.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  bool _matchReminders = true;
  bool _tournamentAlerts = true;
  bool _weeklySummary = false;
  bool _soundEffects = true;
  bool _haptics = true;
  final bool _darkMode = false;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
      children: [
        ProfileHeaderCard(
          name: 'Toni Roig',
          subtitle: 'Jugador desde junio de 2026 · Liga Élite',
          onEditTap: () {}, // pendiente: pantalla de edición de perfil
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
              trailingWidget: Switch(
                value: _matchReminders,
                onChanged: (v) => setState(() => _matchReminders = v),
              ),
            ),
            SettingsTile(
              icon: Icons.emoji_events_outlined,
              title: 'Alertas de torneos',
              trailingWidget: Switch(
                value: _tournamentAlerts,
                onChanged: (v) => setState(() => _tournamentAlerts = v),
              ),
            ),
            SettingsTile(
              icon: Icons.mail_outline_rounded,
              title: 'Resumen semanal',
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
            const SettingsTile(
              icon: Icons.public_rounded,
              title: 'Zona horaria',
              trailingText: 'Europe/Madrid (UTC+2)',
            ),
            SettingsTile(
              icon: Icons.volume_up_outlined,
              title: 'Efectos de sonido',
              trailingWidget: Switch(
                value: _soundEffects,
                onChanged: (v) => setState(() => _soundEffects = v),
              ),
            ),
            SettingsTile(
              icon: Icons.vibration_rounded,
              title: 'Vibración',
              trailingWidget: Switch(
                value: _haptics,
                onChanged: (v) => setState(() => _haptics = v),
              ),
            ),
            SettingsTile(
              icon: Icons.dark_mode_outlined,
              title: 'Tema oscuro',
              badgeText: 'Próximamente',
              trailingWidget: Switch(value: _darkMode, onChanged: null),
            ),
          ],
        ),
        const SizedBox(height: 20),
        const SettingsSection(
          title: 'Soporte',
          children: [
            SettingsTile(
              icon: Icons.help_outline_rounded,
              title: 'Centro de ayuda',
            ),
            SettingsTile(
              icon: Icons.description_outlined,
              title: 'Términos y privacidad',
            ),
            SettingsTile(
              icon: Icons.support_agent_rounded,
              title: 'Contactar soporte',
            ),
            SettingsTile(
              icon: Icons.info_outline_rounded,
              title: 'Versión de la app',
              trailingText: 'v0.1.0 (beta)',
              showChevron: false,
            ),
          ],
        ),
        const SizedBox(height: 20),
        const SettingsSection(
          title: 'Sesión',
          children: [
            SettingsTile(
              icon: Icons.logout_rounded,
              iconColor: Colors.red,
              title: 'Cerrar sesión',
            ),
          ],
        ),
      ],
    );
  }
}
