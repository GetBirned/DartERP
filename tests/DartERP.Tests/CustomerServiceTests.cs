using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Models;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_WithoutCompanyName_ThrowsValidationException()
    {
        var service = new CustomerService(new FakeCustomerRepository());
        var customer = new Customer { CompanyName = string.Empty };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(customer));
    }

    [Fact]
    public async Task CreateAsync_AssignsSequentialCustomerNumber()
    {
        var service = new CustomerService(new FakeCustomerRepository());

        var first = await service.CreateAsync(new Customer { CompanyName = "Granite State Sporting Supply" });
        var second = await service.CreateAsync(new Customer { CompanyName = "Atlantic Sporting Goods" });

        Assert.NotEqual(first.CustomerNumber, second.CustomerNumber);
        Assert.True(first.IsActive);
    }
}
