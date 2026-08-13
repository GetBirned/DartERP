using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakeSerializedItemRepository : ISerializedItemRepository
{
    private readonly List<SerializedItem> _items = [];
    private int _nextId = 1;

    public FakeSerializedItemRepository(IEnumerable<SerializedItem>? seed = null)
    {
        foreach (var item in seed ?? [])
        {
            item.SerializedItemId = _nextId++;
            _items.Add(item);
        }
    }

    public Task<SerializedItem?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(i => i.SerializedItemId == id));

    public Task<List<SerializedItem>> GetAllAsync() => Task.FromResult(_items.ToList());

    public Task AddAsync(SerializedItem entity)
    {
        entity.SerializedItemId = _nextId++;
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SerializedItem entity) => Task.CompletedTask;

    public Task<List<SerializedItem>> GetAllWithDetailsAsync() => Task.FromResult(_items.ToList());

    public Task<List<SerializedItem>> GetByWorkOrderAsync(int workOrderId) =>
        Task.FromResult(_items.Where(i => i.WorkOrderId == workOrderId).ToList());

    public Task<bool> SerialNumberExistsAsync(string serialNumber) =>
        Task.FromResult(_items.Any(i => i.SerialNumber == serialNumber));
}
