using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products = [];
    private int _nextId = 1;

    public FakeProductRepository(IEnumerable<Product>? seed = null)
    {
        foreach (var product in seed ?? [])
        {
            product.ProductId = _nextId++;
            _products.Add(product);
        }
    }

    public Task<Product?> GetByIdAsync(int id) => Task.FromResult(_products.FirstOrDefault(p => p.ProductId == id));

    public Task<List<Product>> GetAllAsync() => Task.FromResult(_products.ToList());

    public Task AddAsync(Product entity)
    {
        entity.ProductId = _nextId++;
        _products.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product entity) => Task.CompletedTask;

    public Task<List<Product>> SearchAsync(string? searchTerm, bool activeOnly) => Task.FromResult(_products.ToList());

    public Task<List<Product>> GetActiveAsync() => Task.FromResult(_products.Where(p => p.IsActive).ToList());

    public Task<List<Product>> GetBelowReorderLevelAsync() =>
        Task.FromResult(_products.Where(p => p.QuantityOnHand <= p.ReorderLevel).ToList());

    public Task<bool> SkuExistsAsync(string sku, int? excludeId = null) =>
        Task.FromResult(_products.Any(p => p.SKU == sku && p.ProductId != excludeId));

    public Task<decimal> GetTotalInventoryValueAsync() =>
        Task.FromResult(_products.Sum(p => p.UnitCost * p.QuantityOnHand));
}
