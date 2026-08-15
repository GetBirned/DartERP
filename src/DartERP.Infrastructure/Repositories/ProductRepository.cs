using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public ProductRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products.FindAsync(id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products.OrderBy(p => p.ProductName).ToListAsync();
    }

    public async Task AddAsync(Product entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Products.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Products.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<Product>> SearchAsync(string? searchTerm, bool activeOnly)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products.AsQueryable();

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

    public async Task<List<Product>> GetActiveAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync();
    }

    public async Task<List<Product>> GetBelowReorderLevelAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .Where(p => p.IsActive && p.QuantityOnHand <= p.ReorderLevel)
            .OrderBy(p => p.QuantityOnHand)
            .ToListAsync();
    }

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products.AnyAsync(p =>
            p.SKU == sku && (excludeId == null || p.ProductId != excludeId));
    }

    public async Task<decimal> GetTotalInventoryValueAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products.SumAsync(p => p.UnitCost * p.QuantityOnHand);
    }

    public async Task<Dictionary<ProductCategory, decimal>> GetInventoryValueByCategoryAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Value = g.Sum(p => p.UnitCost * p.QuantityOnHand) })
            .ToDictionaryAsync(x => x.Category, x => x.Value);
    }
}
