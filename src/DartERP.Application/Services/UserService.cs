using DartERP.Application.Validation;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Core.Security;

namespace DartERP.Application.Services;

public class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public Task<User?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _repository.GetByUsernameAsync(username);

        // Deliberately the same failure for "no such user" and "wrong
        // password" — telling an attacker which one it was is a free hint
        // about which usernames exist. Same reasoning IsActive gets checked
        // here rather than as a separate "account disabled" message.
        if (user is null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<User> RegisterAsync(string username, string email, string password, string displayName, string role, string phone)
    {
        await ValidateUsernameAsync(username, excludeId: null);
        ValidateEmail(email);
        ValidatePassword(password);

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ValidationException("Display name is required.");

        var user = new User
        {
            Username = username.Trim(),
            Email = email.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            DisplayName = displayName.Trim(),
            Role = role.Trim(),
            Phone = phone.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
        };

        await _repository.AddAsync(user);
        return user;
    }

    public async Task UpdateProfileAsync(int userId, string displayName, string role, string phone, string email)
    {
        var user = await _repository.GetByIdAsync(userId)
            ?? throw new ValidationException("This user no longer exists.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ValidationException("Display name is required.");
        ValidateEmail(email);

        user.DisplayName = displayName.Trim();
        user.Role = role.Trim();
        user.Phone = phone.Trim();
        user.Email = email.Trim();

        await _repository.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _repository.GetByIdAsync(userId)
            ?? throw new ValidationException("This user no longer exists.");

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");

        ValidatePassword(newPassword);

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        await _repository.UpdateAsync(user);
    }

    public async Task UpdateProfilePictureAsync(int userId, string? profilePicturePath)
    {
        var user = await _repository.GetByIdAsync(userId)
            ?? throw new ValidationException("This user no longer exists.");

        user.ProfilePicturePath = profilePicturePath;
        await _repository.UpdateAsync(user);
    }

    private async Task ValidateUsernameAsync(string username, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ValidationException("Username is required.");

        if (await _repository.UsernameExistsAsync(username.Trim(), excludeId))
            throw new ValidationException("That username is already taken.");
    }

    private static void ValidateEmail(string email)
    {
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            throw new ValidationException("Enter a valid email address, or leave it blank.");
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");
    }
}
