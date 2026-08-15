using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakePurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly List<PurchaseOrder> _orders = [];
    private int _nextId = 1;
    private int _nextLineId = 1;

    public Task<PurchaseOrder?> GetByIdAsync(int id) => Task.FromResult(_orders.FirstOrDefault(o => o.PurchaseOrderId == id));

    public Task<List<PurchaseOrder>> GetAllAsync() => Task.FromResult(_orders.ToList());

    public Task AddAsync(PurchaseOrder entity)
    {
        entity.PurchaseOrderId = _nextId++;
        foreach (var line in entity.Lines)
            line.PurchaseOrderLineId = _nextLineId++;
        _orders.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PurchaseOrder entity) => Task.CompletedTask;

    public Task<PurchaseOrder?> GetWithLinesAsync(int id) => GetByIdAsync(id);

    public Task<List<PurchaseOrder>> GetAllWithVendorAsync() => Task.FromResult(_orders.ToList());

    public Task<List<PurchaseOrder>> GetRecentAsync(int count) => Task.FromResult(_orders.Take(count).ToList());

    public Task<bool> PurchaseOrderNumberExistsAsync(string purchaseOrderNumber) =>
        Task.FromResult(_orders.Any(o => o.PurchaseOrderNumber == purchaseOrderNumber));

    public Task<string> GetNextPurchaseOrderNumberAsync() => Task.FromResult($"PO-{10000 + _orders.Count + 1}");

    public Task<int> GetOpenCountAsync() =>
        Task.FromResult(_orders.Count(o => o.Status != Core.Enums.PurchaseOrderStatus.Received && o.Status != Core.Enums.PurchaseOrderStatus.Cancelled));

    public Task<Dictionary<Core.Enums.PurchaseOrderStatus, int>> GetCountsByStatusAsync() =>
        Task.FromResult(_orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()));

    public Task UpdateWithLinesAsync(PurchaseOrder header, List<PurchaseOrderLine> lines)
    {
        var existing = _orders.First(o => o.PurchaseOrderId == header.PurchaseOrderId);
        existing.VendorId = header.VendorId;
        existing.ExpectedDate = header.ExpectedDate;
        existing.Status = header.Status;
        existing.Notes = header.Notes;
        existing.TotalAmount = header.TotalAmount;
        existing.Lines = lines;
        return Task.CompletedTask;
    }
}
