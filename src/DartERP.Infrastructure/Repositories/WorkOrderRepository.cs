using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class WorkOrderRepository : IWorkOrderRepository
{
    private readonly DartErpDbContext _context;

    public WorkOrderRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<WorkOrder?> GetByIdAsync(int id) =>
        await _context.WorkOrders.Include(w => w.Product).FirstOrDefaultAsync(w => w.WorkOrderId == id);

    public async Task<List<WorkOrder>> GetAllAsync() =>
        await _context.WorkOrders.OrderByDescending(w => w.StartDate).ToListAsync();

    public async Task AddAsync(WorkOrder entity) => await _context.WorkOrders.AddAsync(entity);

    public void Update(WorkOrder entity) => _context.WorkOrders.Update(entity);

    public async Task<List<WorkOrder>> GetAllWithProductAsync() =>
        await _context.WorkOrders
            .Include(w => w.Product)
            .OrderByDescending(w => w.StartDate)
            .ToListAsync();

    public async Task<List<WorkOrder>> GetDueSoonAsync(int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await _context.WorkOrders
            .Include(w => w.Product)
            .Where(w => w.DueDate <= cutoff &&
                        w.Status != WorkOrderStatus.Completed &&
                        w.Status != WorkOrderStatus.Cancelled)
            .OrderBy(w => w.DueDate)
            .ToListAsync();
    }

    public async Task<string> GetNextWorkOrderNumberAsync()
    {
        var numbers = await _context.WorkOrders.Select(w => w.WorkOrderNumber).ToListAsync();

        var nextSeq = numbers
            .Select(n => int.TryParse(n.Replace("WO-", ""), out var seq) ? seq : 0)
            .DefaultIfEmpty(10000)
            .Max() + 1;

        return $"WO-{nextSeq}";
    }

    public async Task<int> GetOpenCountAsync() =>
        await _context.WorkOrders.CountAsync(w =>
            w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled);

    public async Task<int> GetUnitsInProductionAsync() =>
        await _context.WorkOrders
            .Where(w => w.Status == WorkOrderStatus.Released || w.Status == WorkOrderStatus.InProduction)
            .SumAsync(w => w.Quantity);
}
