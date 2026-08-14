using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public FakeUserRepository(IEnumerable<User>? seed = null)
    {
        foreach (var user in seed ?? [])
        {
            user.UserId = _nextId++;
            _users.Add(user);
        }
    }

    public Task<User?> GetByIdAsync(int id) => Task.FromResult(_users.FirstOrDefault(u => u.UserId == id));

    public Task<List<User>> GetAllAsync() => Task.FromResult(_users.ToList());

    public Task AddAsync(User entity)
    {
        entity.UserId = _nextId++;
        _users.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User entity) => Task.CompletedTask;

    public Task<User?> GetByUsernameAsync(string username) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Username == username));

    public Task<bool> UsernameExistsAsync(string username, int? excludeId = null) =>
        Task.FromResult(_users.Any(u => u.Username == username && u.UserId != excludeId));
}
