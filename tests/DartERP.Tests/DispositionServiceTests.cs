using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class DispositionServiceTests
{
    private static (DispositionService Service, SerializedItem Item, Customer Customer) CreateService()
    {
        var item = new SerializedItem { SerialNumber = "DERP-2026-000001", Status = SerializedItemStatus.InStock };
        var items = new FakeSerializedItemRepository([item]);
        var dispositions = new FakeDispositionRepository();
        var customer = new Customer { CustomerId = 1, CompanyName = "Granite State Sporting Supply" };

        return (new DispositionService(dispositions, items), item, customer);
    }

    [Fact]
    public async Task CreateAsync_SoldWithoutCustomer_ThrowsValidationException()
    {
        var (service, item, _) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(item.SerializedItemId, DateTime.Today, DispositionType.Sold, customerId: null, string.Empty));
    }

    [Fact]
    public async Task CreateAsync_TransferredWithoutCustomer_ThrowsValidationException()
    {
        var (service, item, _) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(item.SerializedItemId, DateTime.Today, DispositionType.Transferred, customerId: null, string.Empty));
    }

    [Fact]
    public async Task CreateAsync_DestroyedWithoutCustomer_Succeeds()
    {
        var (service, item, _) = CreateService();

        var disposition = await service.CreateAsync(item.SerializedItemId, DateTime.Today, DispositionType.Destroyed, customerId: null, string.Empty);

        Assert.Null(disposition.CustomerId);
    }

    [Fact]
    public async Task CreateAsync_Sold_UpdatesSerializedItemStatusToShipped()
    {
        var (service, item, customer) = CreateService();

        await service.CreateAsync(item.SerializedItemId, DateTime.Today, DispositionType.Sold, customer.CustomerId, string.Empty);

        Assert.Equal(SerializedItemStatus.Shipped, item.Status);
    }

    [Fact]
    public async Task CreateAsync_Destroyed_UpdatesSerializedItemStatusToScrapped()
    {
        var (service, item, _) = CreateService();

        await service.CreateAsync(item.SerializedItemId, DateTime.Today, DispositionType.Destroyed, customerId: null, string.Empty);

        Assert.Equal(SerializedItemStatus.Scrapped, item.Status);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownSerializedItem_ThrowsValidationException()
    {
        var (service, _, customer) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(999, DateTime.Today, DispositionType.Sold, customer.CustomerId, string.Empty));
    }
}
