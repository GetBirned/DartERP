using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class QualityInspectionRepository : IQualityInspectionRepository
{
    private readonly DartErpDbContext _context;

    public QualityInspectionRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<QualityInspection?> GetByIdAsync(int id) =>
        await _context.QualityInspections
            .Include(q => q.SerializedItem)
                .ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(q => q.QualityInspectionId == id);

    public async Task<List<QualityInspection>> GetAllAsync() =>
        await _context.QualityInspections.OrderByDescending(q => q.InspectionDate).ToListAsync();

    public async Task AddAsync(QualityInspection entity) => await _context.QualityInspections.AddAsync(entity);

    public void Update(QualityInspection entity) => _context.QualityInspections.Update(entity);

    public async Task<List<QualityInspection>> GetAllWithDetailsAsync() =>
        await _context.QualityInspections
            .Include(q => q.SerializedItem)
                .ThenInclude(s => s!.Product)
            .OrderByDescending(q => q.InspectionDate)
            .ToListAsync();

    public async Task<List<QualityInspection>> GetPendingAsync() =>
        await _context.QualityInspections
            .Include(q => q.SerializedItem)
                .ThenInclude(s => s!.Product)
            .Where(q => q.Result == QualityResult.Pending)
            .OrderBy(q => q.InspectionDate)
            .ToListAsync();

    public async Task<int> GetPendingCountAsync() =>
        await _context.QualityInspections.CountAsync(q => q.Result == QualityResult.Pending);
}
