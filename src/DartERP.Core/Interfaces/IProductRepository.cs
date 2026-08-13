using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<List<Product>> SearchAsync(string? searchTerm, bool activeOnly);
    Task<List<Product>> GetActiveAsync();
    Task<List<Product>> GetBelowReorderLevelAsync();
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    Task<decimal> GetTotalInventoryValueAsync();
}
