using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class DispositionRepository : IDispositionRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public DispositionRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Disposition?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Dispositions
            .Include(d => d.SerializedItem)
                .ThenInclude(s => s!.Product)
            .Include(d => d.Customer)
            .FirstOrDefaultAsync(d => d.DispositionId == id);
    }

    public async Task<List<Disposition>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Dispositions.OrderByDescending(d => d.DispositionDate).ToListAsync();
    }

    public async Task AddAsync(Disposition entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Dispositions.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Disposition entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Dispositions.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<Disposition>> GetAllWithDetailsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Dispositions
            .Include(d => d.SerializedItem)
                .ThenInclude(s => s!.Product)
            .Include(d => d.Customer)
            .OrderByDescending(d => d.DispositionDate)
            .ToListAsync();
    }

    public async Task<List<Disposition>> GetForSerializedItemAsync(int serializedItemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Dispositions
            .Include(d => d.Customer)
            .Where(d => d.SerializedItemId == serializedItemId)
            .OrderByDescending(d => d.DispositionDate)
            .ToListAsync();
    }
}
