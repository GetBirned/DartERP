using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class WorkOrderServiceTests
{
    private static (WorkOrderService Service, FakeProductRepository Products) CreateService()
    {
        var products = new FakeProductRepository(
        [
            new Product { SKU = "DERP-1001", ProductName = "Model Alpha", IsActive = true, IsSerialized = true },
        ]);
        var workOrders = new FakeWorkOrderRepository();
        return (new WorkOrderService(workOrders, products), products);
    }

    [Fact]
    public async Task CreateAsync_WithDueDateBeforeStartDate_ThrowsValidationException()
    {
        var (service, products) = CreateService();
        var product = (await products.GetAllAsync()).First();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(product.ProductId, 10, DateTime.Today, DateTime.Today.AddDays(-1), string.Empty, WorkOrderStatus.Planned));
    }

    [Fact]
    public async Task CreateAsync_WithZeroQuantity_ThrowsValidationException()
    {
        var (service, products) = CreateService();
        var product = (await products.GetAllAsync()).First();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(product.ProductId, 0, DateTime.Today, DateTime.Today.AddDays(7), string.Empty, WorkOrderStatus.Planned));
    }

    [Fact]
    public async Task UpdateAsync_OnCompletedWorkOrder_ThrowsValidationException()
    {
        var (service, products) = CreateService();
        var product = (await products.GetAllAsync()).First();
        var completedOrder = new WorkOrder
        {
            WorkOrderId = 99,
            ProductId = product.ProductId,
            Quantity = 5,
            StartDate = DateTime.Today.AddDays(-10),
            DueDate = DateTime.Today.AddDays(-1),
            Status = WorkOrderStatus.Completed,
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(completedOrder, product.ProductId, 6, DateTime.Today, DateTime.Today.AddDays(5), string.Empty, WorkOrderStatus.InProduction));
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed, true)]
    [InlineData(WorkOrderStatus.InProduction, false)]
    public void IsLocked_ReflectsTerminalStatuses(WorkOrderStatus status, bool expectedLocked)
    {
        Assert.Equal(expectedLocked, WorkOrderService.IsLocked(status));
    }
}
