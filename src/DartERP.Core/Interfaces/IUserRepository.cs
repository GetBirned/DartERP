using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username, int? excludeId = null);
}
