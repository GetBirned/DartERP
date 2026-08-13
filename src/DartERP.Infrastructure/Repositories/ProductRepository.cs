using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DartErpDbContext _context;

    public ProductRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id) =>
        await _context.Products.FindAsync(id);

    public async Task<List<Product>> GetAllAsync() =>
        await _context.Products.OrderBy(p => p.ProductName).ToListAsync();

    public async Task AddAsync(Product entity) => await _context.Products.AddAsync(entity);

    public void Update(Product entity) => _context.Products.Update(entity);

    public async Task<List<Product>> SearchAsync(string? searchTerm, bool activeOnly)
    {
        var query = _context.Products.AsQueryable();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p =>
                p.ProductName.Contains(term) ||
                p.SKU.Contains(term));
        }

        return await query.OrderBy(p => p.ProductName).ToListAsync();
    }

    public async Task<List<Product>> GetActiveAsync() =>
        await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync();

    public async Task<List<Product>> GetBelowReorderLevelAsync() =>
        await _context.Products
            .Where(p => p.IsActive && p.QuantityOnHand <= p.ReorderLevel)
            .OrderBy(p => p.QuantityOnHand)
            .ToListAsync();

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null) =>
        await _context.Products.AnyAsync(p =>
            p.SKU == sku && (excludeId == null || p.ProductId != excludeId));

    public async Task<decimal> GetTotalInventoryValueAsync() =>
        await _context.Products.SumAsync(p => p.UnitCost * p.QuantityOnHand);
}
