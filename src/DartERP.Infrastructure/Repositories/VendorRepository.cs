using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public VendorRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Vendor?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vendors.FindAsync(id);
    }

    public async Task<List<Vendor>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vendors.OrderBy(v => v.CompanyName).ToListAsync();
    }

    public async Task AddAsync(Vendor entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Vendors.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Vendor entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Vendors.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<Vendor>> SearchAsync(string? searchTerm, bool activeOnly)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Vendors.AsQueryable();

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(v =>
                v.CompanyName.Contains(term) ||
                v.VendorNumber.Contains(term) ||
                v.ContactName.Contains(term));
        }

        return await query.OrderBy(v => v.CompanyName).ToListAsync();
    }

    public async Task<List<Vendor>> GetActiveAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vendors.Where(v => v.IsActive).OrderBy(v => v.CompanyName).ToListAsync();
    }

    public async Task<bool> VendorNumberExistsAsync(string vendorNumber, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vendors.AnyAsync(v =>
            v.VendorNumber == vendorNumber && (excludeId == null || v.VendorId != excludeId));
    }

    public async Task<int> GetActiveCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vendors.CountAsync(v => v.IsActive);
    }

    public async Task<string> GetNextVendorNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var numbers = await context.Vendors.Select(v => v.VendorNumber).ToListAsync();

        var nextSeq = numbers
            .Select(n => int.TryParse(n.Replace("VEND-", ""), out var seq) ? seq : 0)
            .DefaultIfEmpty(2000)
            .Max() + 1;

        return $"VEND-{nextSeq}";
    }
}
