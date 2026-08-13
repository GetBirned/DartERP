using DartERP.Application.Validation;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class VendorService
{
    private readonly IVendorRepository _repository;

    public VendorService(IVendorRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Vendor>> SearchAsync(string? searchTerm, bool activeOnly) =>
        _repository.SearchAsync(searchTerm, activeOnly);

    public Task<List<Vendor>> GetActiveAsync() => _repository.GetActiveAsync();

    public Task<Vendor?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<int> GetActiveCountAsync() => _repository.GetActiveCountAsync();

    public async Task<Vendor> CreateAsync(Vendor vendor)
    {
        Validate(vendor);

        vendor.VendorNumber = await _repository.GetNextVendorNumberAsync();
        vendor.CreatedDate = DateTime.UtcNow;
        vendor.IsActive = true;

        await _repository.AddAsync(vendor);
        return vendor;
    }

    public async Task UpdateAsync(Vendor vendor)
    {
        Validate(vendor);
        await _repository.UpdateAsync(vendor);
    }

    public async Task SetActiveStatusAsync(int vendorId, bool isActive)
    {
        var vendor = await _repository.GetByIdAsync(vendorId)
            ?? throw new ValidationException("This vendor no longer exists.");

        vendor.IsActive = isActive;
        await _repository.UpdateAsync(vendor);
    }

    private static void Validate(Vendor vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor.CompanyName))
            throw new ValidationException("Company name is required.");

        if (!string.IsNullOrWhiteSpace(vendor.Email) && !vendor.Email.Contains('@'))
            throw new ValidationException("Enter a valid email address, or leave it blank.");
    }
}
