using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface ISerializedItemRepository : IRepository<SerializedItem>
{
    Task<List<SerializedItem>> GetAllWithDetailsAsync();
    Task<List<SerializedItem>> GetByWorkOrderAsync(int workOrderId);
    Task<bool> SerialNumberExistsAsync(string serialNumber);
}
