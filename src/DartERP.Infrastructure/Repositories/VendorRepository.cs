using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly DartErpDbContext _context;

    public VendorRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<Vendor?> GetByIdAsync(int id) =>
        await _context.Vendors.FindAsync(id);

    public async Task<List<Vendor>> GetAllAsync() =>
        await _context.Vendors.OrderBy(v => v.CompanyName).ToListAsync();

    public async Task AddAsync(Vendor entity) => await _context.Vendors.AddAsync(entity);

    public void Update(Vendor entity) => _context.Vendors.Update(entity);

    public async Task<List<Vendor>> SearchAsync(string? searchTerm, bool activeOnly)
    {
        var query = _context.Vendors.AsQueryable();

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

    public async Task<List<Vendor>> GetActiveAsync() =>
        await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.CompanyName).ToListAsync();

    public async Task<bool> VendorNumberExistsAsync(string vendorNumber, int? excludeId = null) =>
        await _context.Vendors.AnyAsync(v =>
            v.VendorNumber == vendorNumber && (excludeId == null || v.VendorId != excludeId));

    public async Task<int> GetActiveCountAsync() =>
        await _context.Vendors.CountAsync(v => v.IsActive);
}
