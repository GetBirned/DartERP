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
}
