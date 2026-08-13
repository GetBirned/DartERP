using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly DartErpDbContext _context;

    public PurchaseOrderRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id) =>
        await _context.PurchaseOrders.FindAsync(id);

    public async Task<List<PurchaseOrder>> GetAllAsync() =>
        await _context.PurchaseOrders.OrderByDescending(po => po.OrderDate).ToListAsync();

    public async Task AddAsync(PurchaseOrder entity) => await _context.PurchaseOrders.AddAsync(entity);

    public void Update(PurchaseOrder entity) => _context.PurchaseOrders.Update(entity);

    public async Task<PurchaseOrder?> GetWithLinesAsync(int id) =>
        await _context.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

    public async Task<List<PurchaseOrder>> GetAllWithVendorAsync() =>
        await _context.PurchaseOrders
            .Include(po => po.Vendor)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();

    public async Task<List<PurchaseOrder>> GetRecentAsync(int count) =>
        await _context.PurchaseOrders
            .Include(po => po.Vendor)
            .OrderByDescending(po => po.OrderDate)
            .Take(count)
            .ToListAsync();

    public async Task<bool> PurchaseOrderNumberExistsAsync(string purchaseOrderNumber) =>
        await _context.PurchaseOrders.AnyAsync(po => po.PurchaseOrderNumber == purchaseOrderNumber);

    public async Task<string> GetNextPurchaseOrderNumberAsync()
    {
        var maxNumber = await _context.PurchaseOrders
            .Select(po => po.PurchaseOrderNumber)
            .ToListAsync();

        var nextSeq = maxNumber
            .Select(n => int.TryParse(n.Replace("PO-", ""), out var seq) ? seq : 0)
            .DefaultIfEmpty(10000)
            .Max() + 1;

        return $"PO-{nextSeq}";
    }

    public async Task<int> GetOpenCountAsync() =>
        await _context.PurchaseOrders.CountAsync(po =>
            po.Status != PurchaseOrderStatus.Received && po.Status != PurchaseOrderStatus.Cancelled);
}
