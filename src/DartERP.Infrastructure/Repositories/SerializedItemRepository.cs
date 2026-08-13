using DartERP.Core.Interfaces;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Repositories;

public class SerializedItemRepository : ISerializedItemRepository
{
    private readonly DartErpDbContext _context;

    public SerializedItemRepository(DartErpDbContext context)
    {
        _context = context;
    }

    public async Task<SerializedItem?> GetByIdAsync(int id) =>
        await _context.SerializedItems
            .Include(s => s.Product)
            .Include(s => s.WorkOrder)
            .FirstOrDefaultAsync(s => s.SerializedItemId == id);

    public async Task<List<SerializedItem>> GetAllAsync() =>
        await _context.SerializedItems.OrderByDescending(s => s.CreatedDate).ToListAsync();

    public async Task AddAsync(SerializedItem entity) => await _context.SerializedItems.AddAsync(entity);

    public void Update(SerializedItem entity) => _context.SerializedItems.Update(entity);

    public async Task<List<SerializedItem>> GetAllWithDetailsAsync() =>
        await _context.SerializedItems
            .Include(s => s.Product)
            .Include(s => s.WorkOrder)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();

    public async Task<List<SerializedItem>> GetByWorkOrderAsync(int workOrderId) =>
        await _context.SerializedItems
            .Include(s => s.Product)
            .Where(s => s.WorkOrderId == workOrderId)
            .OrderBy(s => s.SerialNumber)
            .ToListAsync();

    public async Task<bool> SerialNumberExistsAsync(string serialNumber) =>
        await _context.SerializedItems.AnyAsync(s => s.SerialNumber == serialNumber);
}
