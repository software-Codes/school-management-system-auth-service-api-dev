using System.Security.Cryptography;
using AuthService.Abstractions.Security;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Security;


public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private readonly ILogger<Argon2PasswordHasher> _logger;

    private const int SaltSize = 16;              
    private const int HashSize = 32;              
    private const int MemorySize = 65536;         
    private const int Iterations = 4;             
    private const int DegreeOfParallelism = 1;    

    public Argon2PasswordHasher(ILogger<Argon2PasswordHasher> logger)
    {
        _logger = logger;
    }

    
    public byte[] HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        try
        {
            var salt = GenerateSalt();

            var hash = HashPasswordInternal(password, salt);

            var result = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

            _logger.LogDebug("Password hashed successfully using Argon2id");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while hashing password");
            throw;
        }
    }

    public bool VerifyPassword(string password, byte[] storedHash)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (storedHash == null || storedHash.Length != SaltSize + HashSize)
        {
            _logger.LogWarning("Invalid stored hash format. Expected {ExpectedLength} bytes, got {ActualLength} bytes",
                SaltSize + HashSize, storedHash?.Length ?? 0);
            return false;
        }

        try
        {
            var salt = new byte[SaltSize];
            var hash = new byte[HashSize];
            
            Buffer.BlockCopy(storedHash, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(storedHash, SaltSize, hash, 0, HashSize);

            var newHash = HashPasswordInternal(password, salt);

            var isValid = CryptographicOperations.FixedTimeEquals(hash, newHash);

            if (!isValid)
            {
                _logger.LogWarning("Password verification failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while verifying password");
            return false;
        }
    }

    private byte[] HashPasswordInternal(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        return argon2.GetBytes(HashSize);
    }

    private byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
}

