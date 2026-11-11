using System.Security.Cryptography;
using GestionReservas.Application.Common.Interfaces;

namespace GestionReservas.Application.Services;

/// <summary>
/// Implementación criptográfica estándar de hash de contraseñas mediante PBKDF2 con HMAC-SHA256 y sal aleatoria.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32;  // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize
        );

        return string.Join(
            SegmentDelimiter,
            Convert.ToHexString(hash),
            Convert.ToHexString(salt),
            Iterations,
            Algorithm.Name
        );
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        string[] segments = passwordHash.Split(SegmentDelimiter);
        if (segments.Length != 4)
        {
            return false;
        }

        byte[] hash = Convert.FromHexString(segments[0]);
        byte[] salt = Convert.FromHexString(segments[1]);
        int iterations = int.Parse(segments[2]);
        var algorithm = new HashAlgorithmName(segments[3]);

        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            algorithm,
            hash.Length
        );

        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}
