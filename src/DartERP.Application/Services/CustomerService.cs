using DartERP.Application.Validation;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class CustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Customer>> SearchAsync(string? searchTerm, bool activeOnly) =>
        _repository.SearchAsync(searchTerm, activeOnly);

    public Task<Customer?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<int> GetActiveCountAsync() => _repository.GetActiveCountAsync();

    public async Task<Customer> CreateAsync(Customer customer)
    {
        Validate(customer);

        customer.CustomerNumber = await _repository.GetNextCustomerNumberAsync();
        customer.CreatedDate = DateTime.UtcNow;
        customer.IsActive = true;

        await _repository.AddAsync(customer);
        return customer;
    }

    public async Task UpdateAsync(Customer customer)
    {
        Validate(customer);
        await _repository.UpdateAsync(customer);
    }

    public async Task SetActiveStatusAsync(int customerId, bool isActive)
    {
        var customer = await _repository.GetByIdAsync(customerId)
            ?? throw new ValidationException("This customer no longer exists.");

        customer.IsActive = isActive;
        await _repository.UpdateAsync(customer);
    }

    private static void Validate(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CompanyName))
            throw new ValidationException("Company name is required.");

        if (!string.IsNullOrWhiteSpace(customer.Email) && !customer.Email.Contains('@'))
            throw new ValidationException("Enter a valid email address, or leave it blank.");
    }
}
