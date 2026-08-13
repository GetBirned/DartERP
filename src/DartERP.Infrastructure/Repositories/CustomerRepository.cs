using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DartErpDbContext _context;

    public CustomerRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id) =>
        await _context.Customers.FindAsync(id);

    public async Task<List<Customer>> GetAllAsync() =>
        await _context.Customers.OrderBy(c => c.CompanyName).ToListAsync();

    public async Task AddAsync(Customer entity) => await _context.Customers.AddAsync(entity);

    public void Update(Customer entity) => _context.Customers.Update(entity);

    public async Task<List<Customer>> SearchAsync(string? searchTerm, bool activeOnly)
    {
        var query = _context.Customers.AsQueryable();

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

    public async Task<bool> CustomerNumberExistsAsync(string customerNumber, int? excludeId = null) =>
        await _context.Customers.AnyAsync(c =>
            c.CustomerNumber == customerNumber && (excludeId == null || c.CustomerId != excludeId));

    public async Task<int> GetActiveCountAsync() =>
        await _context.Customers.CountAsync(c => c.IsActive);
}
