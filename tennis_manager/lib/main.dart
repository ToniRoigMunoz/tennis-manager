import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:connectivity_plus/connectivity_plus.dart';
import 'dart:convert';
import 'config.dart';

void main() {
  runApp(const TennisManagerApp());
}

class TennisManagerApp extends StatelessWidget {
  const TennisManagerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Tennis Manager',
      theme: ThemeData(colorSchemeSeed: Colors.green, useMaterial3: true),
      home: const HomeScreen(),
    );
  }
}

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  String _serverTime = 'Pulsa el botón para consultar el servidor';
  bool _loading = false;

  Future<void> _fetchServerTime() async {
    // Comprobar conexión
    final connectivity = await Connectivity().checkConnectivity();
    if (connectivity.contains(ConnectivityResult.none)) {
      setState(
        () => _serverTime = 'Sin conexión. Este juego requiere internet.',
      );
      return;
    }

    setState(() => _loading = true);

    try {
      final response = await http
          .get(Uri.parse('${Config.apiBaseUrl}/GetServerTime'))
          .timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(
          () => _serverTime =
              '${data['message']}\n\nHora UTC: ${data['serverTime']}',
        );
      } else {
        setState(
          () => _serverTime = 'Error del servidor: ${response.statusCode}',
        );
      }
    } catch (e) {
      setState(() => _serverTime = 'No se pudo conectar con el servidor:\n$e');
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Tennis Manager')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              _serverTime,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            const SizedBox(height: 32),
            _loading
                ? const CircularProgressIndicator()
                : ElevatedButton.icon(
                    onPressed: _fetchServerTime,
                    icon: const Icon(Icons.cloud_sync),
                    label: const Text('Consultar servidor'),
                  ),
          ],
        ),
      ),
    );
  }
}
