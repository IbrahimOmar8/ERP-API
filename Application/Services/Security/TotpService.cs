using System.Security.Cryptography;
using System.Text;

namespace Application.Services.Security
{
    // RFC 6238 TOTP implementation. Default: 6-digit code, 30-second step,
    // HMAC-SHA1, ±1 step tolerance for clock drift.
    public static class TotpService
    {
        private const int Digits = 6;
        private const int StepSeconds = 30;

        // Generate a base32-encoded 20-byte secret suitable for Google
        // Authenticator / Authy. Lowercase letters and 2-7 only.
        public static string GenerateSecret()
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            return Base32Encode(bytes);
        }

        // Build the otpauth:// URI that authenticator apps can scan as QR.
        public static string BuildOtpAuthUri(string issuer, string accountName, string secret)
        {
            var i = Uri.EscapeDataString(issuer);
            var a = Uri.EscapeDataString(accountName);
            return $"otpauth://totp/{i}:{a}?secret={secret}&issuer={i}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
        }

        // Verify a user-supplied code against the secret. Allows ±1 time step
        // (so a 30s code is accepted up to 1 minute either side).
        public static bool Verify(string secret, string code, int windowSteps = 1)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
                return false;
            code = code.Trim();
            if (code.Length != Digits || !code.All(char.IsDigit)) return false;

            byte[] key;
            try { key = Base32Decode(secret); }
            catch { return false; }

            var counter = CurrentCounter();
            for (var w = -windowSteps; w <= windowSteps; w++)
            {
                if (ComputeCode(key, counter + w) == code) return true;
            }
            return false;
        }

        private static long CurrentCounter() =>
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

        private static string ComputeCode(byte[] key, long counter)
        {
            var bytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(bytes);
            var offset = hash[^1] & 0x0F;
            var truncated = ((hash[offset] & 0x7F) << 24)
                          | ((hash[offset + 1] & 0xFF) << 16)
                          | ((hash[offset + 2] & 0xFF) << 8)
                          | (hash[offset + 3] & 0xFF);
            var code = truncated % (int)Math.Pow(10, Digits);
            return code.ToString().PadLeft(Digits, '0');
        }

        // ─── base32 helpers ──────────────────────────────────────────────

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Base32Encode(byte[] data)
        {
            if (data.Length == 0) return string.Empty;
            var sb = new StringBuilder();
            int buffer = data[0];
            int bitsLeft = 8;
            int index = 1;
            while (bitsLeft > 0 || index < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (index < data.Length)
                    {
                        buffer = (buffer << 8) | (data[index] & 0xFF);
                        bitsLeft += 8;
                        index++;
                    }
                    else
                    {
                        var pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }
                var value = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;
                sb.Append(Alphabet[value]);
            }
            return sb.ToString();
        }

        public static byte[] Base32Decode(string input)
        {
            input = input.Trim().TrimEnd('=').ToUpperInvariant().Replace(" ", "");
            if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();
            var output = new List<byte>(input.Length * 5 / 8);
            int buffer = 0, bitsLeft = 0;
            foreach (var c in input)
            {
                var v = Alphabet.IndexOf(c);
                if (v < 0) throw new FormatException("Invalid base32 character");
                buffer = (buffer << 5) | v;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    output.Add((byte)((buffer >> bitsLeft) & 0xFF));
                }
            }
            return output.ToArray();
        }
    }
}
