using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public UserRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.FirstOrDefaultAsync(u => u.UserId == id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.OrderBy(u => u.DisplayName).ToListAsync();
    }

    public async Task AddAsync(User entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Users.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Users.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.AnyAsync(u => u.Username == username && u.UserId != excludeId);
    }
}
