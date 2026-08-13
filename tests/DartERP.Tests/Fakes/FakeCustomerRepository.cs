using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = [];
    private int _nextId = 1;

    public Task<Customer?> GetByIdAsync(int id) => Task.FromResult(_customers.FirstOrDefault(c => c.CustomerId == id));

    public Task<List<Customer>> GetAllAsync() => Task.FromResult(_customers.ToList());

    public Task AddAsync(Customer entity)
    {
        entity.CustomerId = _nextId++;
        _customers.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer entity) => Task.CompletedTask;

    public Task<List<Customer>> SearchAsync(string? searchTerm, bool activeOnly) => Task.FromResult(_customers.ToList());

    public Task<bool> CustomerNumberExistsAsync(string customerNumber, int? excludeId = null) =>
        Task.FromResult(_customers.Any(c => c.CustomerNumber == customerNumber && c.CustomerId != excludeId));

    public Task<string> GetNextCustomerNumberAsync() => Task.FromResult($"CUST-{1000 + _customers.Count + 1}");

    public Task<int> GetActiveCountAsync() => Task.FromResult(_customers.Count(c => c.IsActive));
}
