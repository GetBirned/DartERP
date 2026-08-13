using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IVendorRepository : IRepository<Vendor>
{
    Task<List<Vendor>> SearchAsync(string? searchTerm, bool activeOnly);
    Task<List<Vendor>> GetActiveAsync();
    Task<bool> VendorNumberExistsAsync(string vendorNumber, int? excludeId = null);
    Task<string> GetNextVendorNumberAsync();
    Task<int> GetActiveCountAsync();
}
