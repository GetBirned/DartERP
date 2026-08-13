using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class SerializedItemRepository : ISerializedItemRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public SerializedItemRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<SerializedItem?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SerializedItems
            .Include(s => s.Product)
            .Include(s => s.WorkOrder)
            .FirstOrDefaultAsync(s => s.SerializedItemId == id);
    }

    public async Task<List<SerializedItem>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SerializedItems.OrderByDescending(s => s.CreatedDate).ToListAsync();
    }

    public async Task AddAsync(SerializedItem entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.SerializedItems.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SerializedItem entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.SerializedItems.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<SerializedItem>> GetAllWithDetailsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SerializedItems
            .Include(s => s.Product)
            .Include(s => s.WorkOrder)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<SerializedItem>> GetByWorkOrderAsync(int workOrderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SerializedItems
            .Include(s => s.Product)
            .Where(s => s.WorkOrderId == workOrderId)
            .OrderBy(s => s.SerialNumber)
            .ToListAsync();
    }

    public async Task<bool> SerialNumberExistsAsync(string serialNumber)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SerializedItems.AnyAsync(s => s.SerialNumber == serialNumber);
    }
}
