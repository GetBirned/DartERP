using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakeWorkOrderRepository : IWorkOrderRepository
{
    private readonly List<WorkOrder> _workOrders = [];
    private readonly List<WorkOrderStatusHistory> _statusHistory = [];
    private int _nextId = 1;
    private int _nextHistoryId = 1;

    public FakeWorkOrderRepository(IEnumerable<WorkOrder>? seed = null)
    {
        foreach (var workOrder in seed ?? [])
        {
            workOrder.WorkOrderId = _nextId++;
            _workOrders.Add(workOrder);
        }
    }

    public Task<WorkOrder?> GetByIdAsync(int id) => Task.FromResult(_workOrders.FirstOrDefault(w => w.WorkOrderId == id));

    public Task<List<WorkOrder>> GetAllAsync() => Task.FromResult(_workOrders.ToList());

    public Task AddAsync(WorkOrder entity)
    {
        entity.WorkOrderId = _nextId++;
        _workOrders.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkOrder entity) => Task.CompletedTask;

    public Task<List<WorkOrder>> GetAllWithProductAsync() => Task.FromResult(_workOrders.ToList());

    public Task<List<WorkOrder>> GetDueSoonAsync(int days) => Task.FromResult(_workOrders.ToList());

    public Task<string> GetNextWorkOrderNumberAsync() => Task.FromResult($"WO-{10000 + _workOrders.Count + 1}");

    public Task<int> GetOpenCountAsync() =>
        Task.FromResult(_workOrders.Count(w => w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled));

    public Task<int> GetUnitsInProductionAsync() =>
        Task.FromResult(_workOrders.Where(w => w.Status is WorkOrderStatus.Released or WorkOrderStatus.InProduction).Sum(w => w.Quantity));

    public Task<List<WorkOrderStatusHistory>> GetStatusHistoryAsync(int workOrderId) =>
        Task.FromResult(_statusHistory.Where(h => h.WorkOrderId == workOrderId).OrderByDescending(h => h.ChangedAt).ToList());

    public Task<List<WorkOrderStatusHistory>> GetAllStatusHistoryAsync() =>
        Task.FromResult(_statusHistory.OrderByDescending(h => h.ChangedAt).ToList());

    public Task AddStatusHistoryAsync(WorkOrderStatusHistory entry)
    {
        entry.WorkOrderStatusHistoryId = _nextHistoryId++;
        _statusHistory.Add(entry);
        return Task.CompletedTask;
    }
}
