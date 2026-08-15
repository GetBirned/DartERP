using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<List<WorkOrder>> GetAllWithProductAsync();
    Task<List<WorkOrder>> GetDueSoonAsync(int days);
    Task<string> GetNextWorkOrderNumberAsync();
    Task<int> GetOpenCountAsync();
    Task<int> GetUnitsInProductionAsync();
    Task<List<WorkOrderStatusHistory>> GetStatusHistoryAsync(int workOrderId);
    Task<List<WorkOrderStatusHistory>> GetAllStatusHistoryAsync();
    Task AddStatusHistoryAsync(WorkOrderStatusHistory entry);
}
