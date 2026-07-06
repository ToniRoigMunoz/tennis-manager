import 'dart:convert';
import 'package:http/http.dart' as http;
import '../config.dart';

class ApiService {
  static Future<Map<String, dynamic>> _get(String path) async {
    final uri = Uri.parse('${Config.apiBaseUrl}/$path');
    final response = await http.get(uri).timeout(const Duration(seconds: 15));
    if (response.statusCode != 200) {
      throw Exception('Error ${response.statusCode} en $path');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  static Future<Map<String, dynamic>> fetchPlayer(String userId) =>
      _get('GetPlayerData?userId=$userId');

  static Future<Map<String, dynamic>> fetchLeague(String leagueId) =>
      _get('GetLeagueData?leagueId=$leagueId');

  static Future<Map<String, dynamic>> fetchTournaments(String seasonId) =>
      _get('GetTournamentData?seasonId=$seasonId');

  static Future<Map<String, dynamic>> fetchDashboard(String userId) =>
      _get('GetDashboardData?userId=$userId');
}
