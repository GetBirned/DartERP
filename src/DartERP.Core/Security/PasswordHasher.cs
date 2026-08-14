using System.Security.Cryptography;

namespace DartERP.Core.Security;

public static class PasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;

    // NOTE: this is hashing, not encryption — encryption is reversible (bad
    // for passwords, since anyone with the key can read them back out).
    // PBKDF2 is one-way: there's no key that turns a hash back into the
    // original password, only a way to check whether a candidate password
    // produces the same hash. The iteration count + a per-user random salt
    // (stored right in the string, not secret) is what makes brute-forcing
    // this slow even if the DB leaks.
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        // Fixed-time comparison — a naive == or SequenceEqual bails out on
        // the first mismatched byte, which leaks timing info an attacker
        // could use to guess the hash one byte at a time. Not a huge risk
        // for a portfolio app, but it's the correct way to compare hashes
        // and costs nothing to do right.
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
