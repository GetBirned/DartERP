using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface IDispositionRepository : IRepository<Disposition>
{
    Task<List<Disposition>> GetAllWithDetailsAsync();
    Task<List<Disposition>> GetForSerializedItemAsync(int serializedItemId);
}
