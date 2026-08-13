using DartERP.Core.Models;

namespace DartERP.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<List<Customer>> SearchAsync(string? searchTerm, bool activeOnly);
    Task<bool> CustomerNumberExistsAsync(string customerNumber, int? excludeId = null);
    Task<string> GetNextCustomerNumberAsync();
    Task<int> GetActiveCountAsync();
}
