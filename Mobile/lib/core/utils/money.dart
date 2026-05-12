import 'package:intl/intl.dart';

class Money {
  static final NumberFormat _fmt =
      NumberFormat.currency(locale: 'ar_EG', symbol: 'ج.م ', decimalDigits: 2);

  static String format(num value) => _fmt.format(value);

  static String number(num value) =>
      NumberFormat.decimalPattern('ar_EG').format(value);
}
