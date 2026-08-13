using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class WorkOrderRepository : IWorkOrderRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public WorkOrderRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<WorkOrder?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WorkOrders.Include(w => w.Product).FirstOrDefaultAsync(w => w.WorkOrderId == id);
    }

    public async Task<List<WorkOrder>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WorkOrders.OrderByDescending(w => w.StartDate).ToListAsync();
    }

    public async Task AddAsync(WorkOrder entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WorkOrders.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkOrder entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WorkOrders.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<WorkOrder>> GetAllWithProductAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WorkOrders
            .Include(w => w.Product)
            .OrderByDescending(w => w.StartDate)
            .ToListAsync();
    }

    public async Task<List<WorkOrder>> GetDueSoonAsync(int days)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await context.WorkOrders
            .Include(w => w.Product)
            .Where(w => w.DueDate <= cutoff &&
                        w.Status != WorkOrderStatus.Completed &&
                        w.Status != WorkOrderStatus.Cancelled)
            .OrderBy(w => w.DueDate)
            .ToListAsync();
    }

    public async Task<string> GetNextWorkOrderNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var numbers = await context.WorkOrders.Select(w => w.WorkOrderNumber).ToListAsync();

        var nextSeq = numbers
            .Select(n => int.TryParse(n.Replace("WO-", ""), out var seq) ? seq : 0)
            .DefaultIfEmpty(10000)
            .Max() + 1;

        return $"WO-{nextSeq}";
    }

    public async Task<int> GetOpenCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WorkOrders.CountAsync(w =>
            w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled);
    }

    public async Task<int> GetUnitsInProductionAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WorkOrders
            .Where(w => w.Status == WorkOrderStatus.Released || w.Status == WorkOrderStatus.InProduction)
            .SumAsync(w => w.Quantity);
    }
}
