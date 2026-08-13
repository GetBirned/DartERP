using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IQualityInspectionRepository : IRepository<QualityInspection>
{
    Task<List<QualityInspection>> GetAllWithDetailsAsync();
    Task<List<QualityInspection>> GetPendingAsync();
    Task<int> GetPendingCountAsync();
}
