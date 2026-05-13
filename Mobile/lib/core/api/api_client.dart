import 'dart:convert';
import 'package:http/http.dart' as http;
import '../config/api_config.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;
  ApiException(this.message, [this.statusCode]);
  @override
  String toString() => 'ApiException($statusCode): $message';
}

class ApiClient {
  String? _token;

  void setToken(String? token) => _token = token;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json; charset=utf-8',
        'Accept': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      };

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final cleanQuery = query?.map(
      (k, v) => MapEntry(k, v?.toString() ?? ''),
    )?..removeWhere((k, v) => v.isEmpty);
    return Uri.parse('${ApiConfig.baseUrl}$path').replace(
      queryParameters: (cleanQuery == null || cleanQuery.isEmpty)
          ? null
          : cleanQuery,
    );
  }

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    final res = await http.get(_uri(path, query), headers: _headers);
    return _handle(res);
  }

  Future<dynamic> post(String path, Object? body) async {
    final res = await http.post(_uri(path),
        headers: _headers, body: jsonEncode(body));
    return _handle(res);
  }

  Future<dynamic> put(String path, Object? body) async {
    final res = await http.put(_uri(path),
        headers: _headers, body: jsonEncode(body));
    return _handle(res);
  }

  Future<dynamic> delete(String path) async {
    final res = await http.delete(_uri(path), headers: _headers);
    return _handle(res);
  }

  Future<List<int>> getBytes(String path) async {
    final res = await http.get(_uri(path), headers: {
      if (_token != null) 'Authorization': 'Bearer $_token',
    });
    if (res.statusCode >= 200 && res.statusCode < 300) return res.bodyBytes;
    throw ApiException('فشل تحميل البيانات', res.statusCode);
  }

  dynamic _handle(http.Response res) {
    if (res.statusCode >= 200 && res.statusCode < 300) {
      if (res.body.isEmpty) return null;
      return jsonDecode(utf8.decode(res.bodyBytes));
    }
    String message = 'حدث خطأ غير متوقع';
    try {
      final body = jsonDecode(utf8.decode(res.bodyBytes));
      if (body is Map && body['error'] != null) message = body['error'];
    } catch (_) {}
    throw ApiException(message, res.statusCode);
  }
}

final apiClient = ApiClient();
