using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

/// <summary>In-memory stand-in so services can be unit tested without a real database.</summary>
public class FakeVendorRepository : IVendorRepository
{
    private readonly List<Vendor> _vendors = [];
    private int _nextId = 1;

    public FakeVendorRepository(IEnumerable<Vendor>? seed = null)
    {
        foreach (var vendor in seed ?? [])
        {
            vendor.VendorId = _nextId++;
            _vendors.Add(vendor);
        }
    }

    public Task<Vendor?> GetByIdAsync(int id) => Task.FromResult(_vendors.FirstOrDefault(v => v.VendorId == id));

    public Task<List<Vendor>> GetAllAsync() => Task.FromResult(_vendors.ToList());

    public Task AddAsync(Vendor entity)
    {
        entity.VendorId = _nextId++;
        _vendors.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Vendor entity) => Task.CompletedTask;

    public Task<List<Vendor>> SearchAsync(string? searchTerm, bool activeOnly) => Task.FromResult(_vendors.ToList());

    public Task<List<Vendor>> GetActiveAsync() => Task.FromResult(_vendors.Where(v => v.IsActive).ToList());

    public Task<bool> VendorNumberExistsAsync(string vendorNumber, int? excludeId = null) =>
        Task.FromResult(_vendors.Any(v => v.VendorNumber == vendorNumber && v.VendorId != excludeId));

    public Task<string> GetNextVendorNumberAsync() => Task.FromResult($"VEND-{2000 + _vendors.Count + 1}");

    public Task<int> GetActiveCountAsync() => Task.FromResult(_vendors.Count(v => v.IsActive));
}
