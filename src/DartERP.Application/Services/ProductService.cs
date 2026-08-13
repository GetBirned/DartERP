using DartERP.Application.Validation;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Product>> SearchAsync(string? searchTerm, bool activeOnly) =>
        _repository.SearchAsync(searchTerm, activeOnly);

    public Task<List<Product>> GetActiveAsync() => _repository.GetActiveAsync();

    public Task<Product?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<Product> CreateAsync(Product product)
    {
        await ValidateAsync(product, excludeId: null);
        product.IsActive = true;
        await _repository.AddAsync(product);
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        await ValidateAsync(product, excludeId: product.ProductId);
        await _repository.UpdateAsync(product);
    }

    public async Task SetActiveStatusAsync(int productId, bool isActive)
    {
        var product = await _repository.GetByIdAsync(productId)
            ?? throw new ValidationException("This product no longer exists.");

        product.IsActive = isActive;
        await _repository.UpdateAsync(product);
    }

    private async Task ValidateAsync(Product product, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(product.SKU))
            throw new ValidationException("SKU is required.");

        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new ValidationException("Product name is required.");

        if (product.UnitCost < 0)
            throw new ValidationException("Unit cost cannot be negative.");

        if (product.SalePrice < 0)
            throw new ValidationException("Sale price cannot be negative.");

        if (product.QuantityOnHand < 0)
            throw new ValidationException("Quantity on hand cannot be negative.");

        if (product.ReorderLevel < 0)
            throw new ValidationException("Reorder level cannot be negative.");

        if (await _repository.SkuExistsAsync(product.SKU, excludeId))
            throw new ValidationException($"SKU '{product.SKU}' is already in use by another product.");
    }
}
