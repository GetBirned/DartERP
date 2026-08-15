using DartERP.Core.Enums;
using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetWithLinesAsync(int id);
    Task<List<PurchaseOrder>> GetAllWithVendorAsync();
    Task<List<PurchaseOrder>> GetRecentAsync(int count);
    Task<bool> PurchaseOrderNumberExistsAsync(string purchaseOrderNumber);
    Task<string> GetNextPurchaseOrderNumberAsync();
    Task<int> GetOpenCountAsync();
    Task<Dictionary<PurchaseOrderStatus, int>> GetCountsByStatusAsync();
    Task<List<PurchaseOrderStatusHistory>> GetStatusHistoryAsync(int purchaseOrderId);
    Task<List<PurchaseOrderStatusHistory>> GetAllStatusHistoryAsync();
    Task AddStatusHistoryAsync(PurchaseOrderStatusHistory entry);

    /// <summary>
    /// Replaces the header fields and full line set for an existing purchase order.
    /// Lines are fully replaced (delete-then-insert) since nothing else references them.
    /// </summary>
    Task UpdateWithLinesAsync(PurchaseOrder header, List<PurchaseOrderLine> lines);
}
