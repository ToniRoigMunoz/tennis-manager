import 'package:flutter/material.dart';
import 'screens/general_screen.dart';
import 'screens/player_screen.dart';
import 'screens/ranking_screen.dart';
import 'screens/tournaments_screen.dart';
import 'screens/settings_screen.dart';
import 'screens/widgets/app_drawer.dart';

void main() {
  runApp(const TennisManagerApp());
}

class TennisManagerApp extends StatelessWidget {
  const TennisManagerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Tennis Manager',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorSchemeSeed: const Color(0xFF2E7D32),
        useMaterial3: true,
        fontFamily: 'Roboto',
      ),
      home: const MainNavigation(),
    );
  }
}

class MainNavigation extends StatefulWidget {
  const MainNavigation({super.key});

  @override
  State<MainNavigation> createState() => _MainNavigationState();
}

class _MainNavigationState extends State<MainNavigation> {
  int _currentIndex = 0;

  static const List<String> _titles = [
    'General',
    'Jugador',
    'Ranking',
    'Torneos',
    'Ajustes',
  ];

  void _onDrawerItemSelected(int index) {
    setState(() => _currentIndex = index);
    Navigator.of(context).pop();
  }

  void _navigateToTab(int index) {
    setState(() => _currentIndex = index);
  }

  @override
  Widget build(BuildContext context) {
    final screens = [
      GeneralScreen(
        onRankingTap: () => _navigateToTab(2),
        onTournamentsTap: () => _navigateToTab(3),
      ),
      const PlayerScreen(),
      const RankingScreen(),
      const TournamentsScreen(),
      const SettingsScreen(),
    ];

    return Scaffold(
      appBar: AppBar(
        title: Text(
          _titles[_currentIndex],
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        centerTitle: false,
        elevation: 0,
      ),
      drawer: AppDrawer(
        selectedIndex: _currentIndex,
        onItemSelected: _onDrawerItemSelected,
      ),
      body: IndexedStack(index: _currentIndex, children: screens),
    );
  }
}
