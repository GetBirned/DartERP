using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public PurchaseOrderRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders.FindAsync(id);
    }

    public async Task<List<PurchaseOrder>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders.OrderByDescending(po => po.OrderDate).ToListAsync();
    }

    public async Task AddAsync(PurchaseOrder entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.PurchaseOrders.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PurchaseOrder entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.PurchaseOrders.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<PurchaseOrder?> GetWithLinesAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);
    }

    public async Task<List<PurchaseOrder>> GetAllWithVendorAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders
            .Include(po => po.Vendor)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();
    }

    public async Task<List<PurchaseOrder>> GetRecentAsync(int count)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders
            .Include(po => po.Vendor)
            .OrderByDescending(po => po.OrderDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> PurchaseOrderNumberExistsAsync(string purchaseOrderNumber)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders.AnyAsync(po => po.PurchaseOrderNumber == purchaseOrderNumber);
    }

    public async Task<string> GetNextPurchaseOrderNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var numbers = await context.PurchaseOrders.Select(po => po.PurchaseOrderNumber).ToListAsync();

        var nextSeq = numbers
            .Select(n => int.TryParse(n.Replace("PO-", ""), out var seq) ? seq : 0)
            .DefaultIfEmpty(10000)
            .Max() + 1;

        return $"PO-{nextSeq}";
    }

    public async Task<int> GetOpenCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders.CountAsync(po =>
            po.Status != PurchaseOrderStatus.Received && po.Status != PurchaseOrderStatus.Cancelled);
    }

    public async Task<Dictionary<PurchaseOrderStatus, int>> GetCountsByStatusAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PurchaseOrders
            .GroupBy(po => po.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task UpdateWithLinesAsync(PurchaseOrder header, List<PurchaseOrderLine> lines)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.PurchaseOrders
            .Include(po => po.Lines)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == header.PurchaseOrderId);

        if (existing is null)
            throw new InvalidOperationException($"Purchase order {header.PurchaseOrderId} not found.");

        existing.VendorId = header.VendorId;
        existing.ExpectedDate = header.ExpectedDate;
        existing.Status = header.Status;
        existing.Notes = header.Notes;
        existing.TotalAmount = header.TotalAmount;

        context.PurchaseOrderLines.RemoveRange(existing.Lines);
        foreach (var line in lines)
        {
            existing.Lines.Add(new PurchaseOrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.LineTotal,
            });
        }

        await context.SaveChangesAsync();
    }
}
