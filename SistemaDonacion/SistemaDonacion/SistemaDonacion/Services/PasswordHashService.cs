using System.Security.Cryptography;
using System.Text;

namespace SistemaDonacion.Services
{
    public interface IPasswordHashService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class PasswordHashService : IPasswordHashService
    {
        /// <summary>
        /// Genera un hash seguro de la contraseña usando PBKDF2.
        /// Formato:"$PBKDF2$iterations$salt$hash"
        /// </summary>
        public string HashPassword(string password)
        {
            const int iterations = 10000;
            const int saltSize = 16; // 128 bits
            const int hashSize = 32; // 256 bits

            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] salt = new byte[saltSize];
                rng.GetBytes(salt);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(hashSize);

                    // Retornar formato: $PBKDF2$iterations$base64(salt)$base64(hash)
                    string saltB64 = Convert.ToBase64String(salt);
                    string hashB64 = Convert.ToBase64String(hash);

                    return $"$PBKDF2${iterations}${saltB64}${hashB64}";
                }
            }
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash.
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                // Parsear el formato: $PBKDF2$iterations$salt$hash
                var parts = hash.Split('$');
                if (parts.Length != 5 || parts[1] != "PBKDF2")
                    return false;

                int iterations = int.Parse(parts[2]);
                byte[] salt = Convert.FromBase64String(parts[3]);
                byte[] storedHash = Convert.FromBase64String(parts[4]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(storedHash.Length);
                    return BytesEqual(computedHash, storedHash);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Comparación segura de bytes.
        /// </summary>
        private bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
