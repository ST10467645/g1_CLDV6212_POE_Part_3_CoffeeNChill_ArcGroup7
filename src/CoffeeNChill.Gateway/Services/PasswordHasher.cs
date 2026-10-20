/*
 * Code Attribution:
 * Hashing a password with BCrypt on register and checking it with Verify on login was adapted from the
 * lecturer-provided CLDV6212 Student Relations gateway example (IIE, 2026) and the BCrypt.Net-Next documentation.
 *
 * BcryptNet (2026a). BCrypt.Net - Bringing updates to the original bcrypt package. [Online]
 * Available at: https://github.com/BcryptNet/bcrypt.net
 * [Accessed 20 October 2026].
 */

namespace CoffeeNChill.Gateway.Services
{
    // Turns a password into a hash and checks a password against a stored hash.
    public static class PasswordHasher
    {
        // Makes the hash that is stored in the Users table.
        // The salt is created inside the hash text, so the same password gives a different hash every time.
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Returns true only when the password matches the stored hash.
        public static bool Verify(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception)
            {
                // If the stored text is not a valid BCrypt hash.
                return false;
            }
        }
    }
}