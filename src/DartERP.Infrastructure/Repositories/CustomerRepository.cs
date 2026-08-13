using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public CustomerRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Customers.FindAsync(id);
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Customers.OrderBy(c => c.CompanyName).ToListAsync();
    }

    public async Task AddAsync(Customer entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Customers.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Customers.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<Customer>> SearchAsync(string? searchTerm, bool activeOnly)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Customers.AsQueryable();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(c =>
                c.CompanyName.Contains(term) ||
                c.CustomerNumber.Contains(term) ||
                c.ContactName.Contains(term));
        }

        return await query.OrderBy(c => c.CompanyName).ToListAsync();
    }

    public async Task<bool> CustomerNumberExistsAsync(string customerNumber, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Customers.AnyAsync(c =>
            c.CustomerNumber == customerNumber && (excludeId == null || c.CustomerId != excludeId));
    }

    public async Task<int> GetActiveCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Customers.CountAsync(c => c.IsActive);
    }
}
