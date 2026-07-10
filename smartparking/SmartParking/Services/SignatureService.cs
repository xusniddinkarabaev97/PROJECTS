using System.Security.Cryptography;
using System.Text;

namespace SmartParking.Services
{
    /// <summary>
    /// HMAC-SHA256 signature validation for payment callbacks and critical API operations.
    /// Implements requirements §3.3 (digital signature) and §4.2 (anti-fraud signature validation).
    /// </summary>
    public interface ISignatureService
    {
        string Sign(string data, string secret);
        bool Verify(string data, string signature, string secret);
        string GenerateApiKey();
    }

    public class SignatureService : ISignatureService
    {
        /// <summary>
        /// Create HMAC-SHA256 signature of data using secret key
        /// </summary>
        public string Sign(string data, string secret)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            var message = Encoding.UTF8.GetBytes(data);
            var hash = HMACSHA256.HashData(key, message);
            return Convert.ToHexStringLower(hash);
        }

        /// <summary>
        /// Verify HMAC-SHA256 signature against data + secret
        /// </summary>
        public bool Verify(string data, string signature, string secret)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            var expected = Sign(data, secret);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature.ToLower()));
        }

        /// <summary>
        /// Generate cryptographically secure API key
        /// </summary>
        public string GenerateApiKey()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "").Replace("/", "").Replace("=", "")[..40];
        }
    }
}
