using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Models;
using DartERP.Core.Security;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class UserServiceTests
{
    private static UserService CreateService(out FakeUserRepository repository, IEnumerable<User>? seed = null)
    {
        repository = new FakeUserRepository(seed);
        return new UserService(repository);
    }

    [Fact]
    public async Task RegisterAsync_WithNewUsername_Succeeds()
    {
        var service = CreateService(out _);

        var user = await service.RegisterAsync("jdoe", "jdoe@example.com", "Password123!", "Jane Doe", "Operator", "603-555-0100");

        Assert.Equal("jdoe", user.Username);
        Assert.NotEqual("Password123!", user.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsValidationException()
    {
        var existing = new User { Username = "jdoe", PasswordHash = PasswordHasher.Hash("Password123!") };
        var service = CreateService(out _, [existing]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterAsync("jdoe", "other@example.com", "Password123!", "Someone Else", "Operator", string.Empty));
    }

    [Fact]
    public async Task AuthenticateAsync_WithCorrectPassword_ReturnsUser()
    {
        var user = new User { Username = "jdoe", PasswordHash = PasswordHasher.Hash("Password123!"), IsActive = true };
        var service = CreateService(out _, [user]);

        var result = await service.AuthenticateAsync("jdoe", "Password123!");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ReturnsNull()
    {
        var user = new User { Username = "jdoe", PasswordHash = PasswordHasher.Hash("Password123!"), IsActive = true };
        var service = CreateService(out _, [user]);

        var result = await service.AuthenticateAsync("jdoe", "WrongPassword!");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ReturnsNull()
    {
        var user = new User { Username = "jdoe", PasswordHash = PasswordHasher.Hash("Password123!"), IsActive = false };
        var service = CreateService(out _, [user]);

        var result = await service.AuthenticateAsync("jdoe", "Password123!");

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsValidationException()
    {
        var user = new User { Username = "jdoe", PasswordHash = PasswordHasher.Hash("Password123!"), IsActive = true };
        var service = CreateService(out _, [user]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ChangePasswordAsync(user.UserId, "WrongCurrent!", "NewPassword123!"));
    }
}
