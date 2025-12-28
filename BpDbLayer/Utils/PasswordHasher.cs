using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace BpDbLayer.Utils
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 4;
        private const int MemorySize = 65536; // 64 MB
        private const int DegreeOfParallelism = 2;

        /// <summary>
        /// Hashes a password using Argon2id
        /// </summary>
        public static string HashPassword(string password)
        {
            // Generate a cryptographically secure random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Hash the password
            byte[] hash = HashPasswordInternal(password, salt);

            // Combine salt and hash, then convert to base64
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                // Decode the hash
                byte[] hashBytes = Convert.FromBase64String(passwordHash);

                if (hashBytes.Length != SaltSize + HashSize)
                {
                    return false;
                }

                // Extract salt
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Extract stored hash
                byte[] storedHash = new byte[HashSize];
                Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);

                // Hash the input password with the same salt
                byte[] computedHash = HashPasswordInternal(password, salt);

                // Compare hashes in constant time to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] HashPasswordInternal(string password, byte[] salt)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations,
            };

            return argon2.GetBytes(HashSize);
        }
    }
}