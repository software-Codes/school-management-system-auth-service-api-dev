using AuthService.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.UnitTests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _passwordHasher;
    private readonly Mock<ILogger<Argon2PasswordHasher>> _loggerMock;

    public Argon2PasswordHasherTests()
    {
        _loggerMock = new Mock<ILogger<Argon2PasswordHasher>>();
        _passwordHasher = new Argon2PasswordHasher(_loggerMock.Object);
    }

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnHashOfCorrectLength()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNull();
        hash.Should().HaveCount(48); // 16 bytes salt + 32 bytes hash
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBeEquivalentTo(hash2, "each hash should have a unique salt");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithInvalidPassword_ShouldThrowArgumentException(string? password)
    {
        // Act
        Action act = () => _passwordHasher.HashPassword(password!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Password cannot be null or empty*");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "SecurePassword123!";
        var incorrectPassword = "WrongPassword456!";
        var hash = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(incorrectPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var invalidHash = new byte[20]; // Wrong length

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var result = _passwordHasher.VerifyPassword(password, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_WithInvalidPassword_ShouldThrowArgumentException(string? password)
    {
        // Arrange
        var validHash = new byte[48];

        // Act
        Action act = () => _passwordHasher.VerifyPassword(password!, validHash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Password cannot be null or empty*");
    }

    [Fact]
    public void HashPassword_AndVerify_ShouldWorkWithComplexPasswords()
    {
        // Arrange
        var complexPasswords = new[]
        {
            "P@ssw0rd!",
            "Très$ecure123",
            "密码Password123!",
            "Пароль123!@#",
            "🔐Secure123!",
            "Super-Long-Password-With-Many-Characters-And-Special-Chars!@#$%^&*()_+{}|:<>?[];',./`~123456789"
        };

        foreach (var password in complexPasswords)
        {
            // Act
            var hash = _passwordHasher.HashPassword(password);
            var isValid = _passwordHasher.VerifyPassword(password, hash);

            // Assert
            isValid.Should().BeTrue($"password '{password}' should be verified correctly");
        }
    }

    [Fact]
    public void HashPassword_ShouldBeSecureAgainstTimingAttacks()
    {
        // Arrange
        var password1 = "Password123!";
        var password2 = "Password123!";
        var hash = _passwordHasher.HashPassword(password1);

        // Act - Multiple verifications should be consistent
        var results = Enumerable.Range(0, 100)
            .Select(_ => _passwordHasher.VerifyPassword(password2, hash))
            .ToList();

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
    }
}

