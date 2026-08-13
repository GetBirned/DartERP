using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class QualityInspectionRepository : IQualityInspectionRepository
{
    private readonly IDbContextFactory<DartErpDbContext> _contextFactory;

    public QualityInspectionRepository(IDbContextFactory<DartErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<QualityInspection?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.QualityInspections
            .Include(q => q.SerializedItem)
                .ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(q => q.QualityInspectionId == id);
    }

    public async Task<List<QualityInspection>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.QualityInspections.OrderByDescending(q => q.InspectionDate).ToListAsync();
    }

    public async Task AddAsync(QualityInspection entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.QualityInspections.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(QualityInspection entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.QualityInspections.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<List<QualityInspection>> GetAllWithDetailsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.QualityInspections
            .Include(q => q.SerializedItem)
                .ThenInclude(s => s!.Product)
            .OrderByDescending(q => q.InspectionDate)
            .ToListAsync();
    }

    public async Task<List<QualityInspection>> GetPendingAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.QualityInspections
            .Include(q => q.SerializedItem)
                .ThenInclude(s => s!.Product)
            .Where(q => q.Result == QualityResult.Pending)
            .OrderBy(q => q.InspectionDate)
            .ToListAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.QualityInspections.CountAsync(q => q.Result == QualityResult.Pending);
    }
}
