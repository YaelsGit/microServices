using Xunit;
using Moq;
using AuthService.Services;
using AuthService.Interfaces;
using AuthService.Repository;
using SharedModels.Models;

namespace AuthService.Tests;

public class UsersServiceTests
{
    private readonly Mock<IUsersRepository> _repositoryMock;
    private readonly UsersService _service;

    public UsersServiceTests()
    {
        _repositoryMock = new Mock<IUsersRepository>();
        _service = new UsersService(_repositoryMock.Object);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("new@example.com"))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        var (success, userId, _) = await _service.RegisterAsync(
            "John", "Doe", "new@example.com", "SecurePass1!", "050-0000000");

        Assert.True(success);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFail()
    {
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("exists@example.com"))
            .ReturnsAsync(new User { Email = "exists@example.com" });

        var (success, _, message) = await _service.RegisterAsync(
            "Jane", "Doe", "exists@example.com", "SecurePass1!", "050-0000000");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashed()
    {
        User? captured = null;
        _repositoryMock.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => captured = u)
            .ReturnsAsync(true);

        await _service.RegisterAsync("A", "B", "a@b.com", "SecurePass1!", "050-0000000");

        Assert.NotNull(captured);
        Assert.NotEqual("SecurePass1!", captured!.PasswordHash);
        Assert.NotEmpty(captured.PasswordHash);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsTokenAndSuccess()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("SecurePass1!");
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("user@example.com"))
            .ReturnsAsync(new User { Id = "1", Email = "user@example.com", PasswordHash = hash });

        var (success, token, _, _) = await _service.LoginAsync("user@example.com", "SecurePass1!");

        Assert.True(success);
        Assert.NotEmpty(token);
        Assert.StartsWith("eyJ", token);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFail()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!");
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("user@example.com"))
            .ReturnsAsync(new User { Id = "1", Email = "user@example.com", PasswordHash = hash });

        var (success, _, _, _) = await _service.LoginAsync("user@example.com", "WrongPass1!");

        Assert.False(success);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFail()
    {
        _repositoryMock.Setup(r => r.GetUserByEmailAsync("ghost@example.com"))
            .ReturnsAsync((User?)null);

        var (success, _, _, _) = await _service.LoginAsync("ghost@example.com", "Pass1!");

        Assert.False(success);
    }

    // ── ValidateTokenAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsFail()
    {
        var (success, _, _) = await _service.ValidateTokenAsync("not-a-jwt");

        Assert.False(success);
    }

    [Fact]
    public async Task ValidateTokenAsync_EmptyToken_ReturnsFail()
    {
        var (success, _, _) = await _service.ValidateTokenAsync(string.Empty);

        Assert.False(success);
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_CorrectOldPassword_ReturnsTrue()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("OldPass1!");
        _repositoryMock.Setup(r => r.GetUserByIdAsync("1"))
            .ReturnsAsync(new User { Id = "1", PasswordHash = hash });
        _repositoryMock.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

        var result = await _service.ChangePasswordAsync(1, "OldPass1!", "NewPass1!");

        Assert.True(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ReturnsFalse()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectOld1!");
        _repositoryMock.Setup(r => r.GetUserByIdAsync("1"))
            .ReturnsAsync(new User { Id = "1", PasswordHash = hash });

        var result = await _service.ChangePasswordAsync(1, "WrongOld1!", "NewPass1!");

        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ReturnsFalse()
    {
        _repositoryMock.Setup(r => r.GetUserByIdAsync("99")).ReturnsAsync((User?)null);

        var result = await _service.ChangePasswordAsync(99, "Any1!", "New1!");

        Assert.False(result);
    }
}
