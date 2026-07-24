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

  static Future<Map<String, dynamic>> fetchUserData(String userId) =>
      _get('GetUserData?userId=$userId');

  static Future<Map<String, dynamic>> fetchPlayer(String userId) =>
      _get('GetPlayerData?userId=$userId');

  static Future<Map<String, dynamic>> fetchLeague(String leagueId) =>
      _get('GetLeagueData?leagueId=$leagueId');

  static Future<Map<String, dynamic>> fetchTournaments(String seasonId) =>
      _get('GetTournamentData?seasonId=$seasonId');

  static Future<Map<String, dynamic>> simulateMatch({
    required String userId,
    required String opponentName,
    int opponentOverall = 72,
    int bestOf = 3,
  }) => _get(
    'SimulateMatch?userId=$userId'
    '&opponentName=${Uri.encodeComponent(opponentName)}'
    '&opponentOverall=$opponentOverall'
    '&bestOf=$bestOf',
  );

  static Future<void> _post(String path, Map<String, dynamic> body) async {
    final uri = Uri.parse('${Config.apiBaseUrl}/$path');
    final response = await http
        .post(
          uri,
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode(body),
        )
        .timeout(const Duration(seconds: 15));
    if (response.statusCode != 200) {
      throw Exception('Error ${response.statusCode} en $path');
    }
  }

  static Future<void> saveMatchResult({
    required String userId,
    required String opponentName,
    required bool won,
    required String setsScore,
    required int aces,
    required int winners,
    required int unforcedErrors,
  }) => _post('SaveMatchResult', {
    'userId': userId,
    'opponentName': opponentName,
    'won': won,
    'setsScore': setsScore,
    'aces': aces,
    'winners': winners,
    'unforcedErrors': unforcedErrors,
  });
}
