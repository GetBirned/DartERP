using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class SerializedItemServiceTests
{
    private static (SerializedItemService Service, WorkOrder SerializedWorkOrder, WorkOrder NonSerializedWorkOrder) CreateService()
    {
        var serializedProduct = new Product { ProductId = 1, SKU = "DERP-1001", ProductName = "Model Alpha", IsSerialized = true };
        var nonSerializedProduct = new Product { ProductId = 2, SKU = "DERP-2001", ProductName = "Steel Billet Stock", IsSerialized = false };

        var serializedWorkOrder = new WorkOrder { WorkOrderId = 1, WorkOrderNumber = "WO-10001", ProductId = 1, Product = serializedProduct, Quantity = 5 };
        var nonSerializedWorkOrder = new WorkOrder { WorkOrderId = 2, WorkOrderNumber = "WO-10002", ProductId = 2, Product = nonSerializedProduct, Quantity = 100 };

        var workOrders = new FakeWorkOrderRepository([serializedWorkOrder, nonSerializedWorkOrder]);
        var items = new FakeSerializedItemRepository();
        return (new SerializedItemService(items, workOrders), serializedWorkOrder, nonSerializedWorkOrder);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSerialNumber_ThrowsValidationException()
    {
        var (service, workOrder, _) = CreateService();
        await service.CreateAsync(workOrder.WorkOrderId, "DERP-2026-000001", SerializedItemStatus.InProduction);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(workOrder.WorkOrderId, "DERP-2026-000001", SerializedItemStatus.InProduction));
    }

    [Fact]
    public async Task CreateAsync_ForNonSerializedProduct_ThrowsValidationException()
    {
        var (service, _, nonSerializedWorkOrder) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(nonSerializedWorkOrder.WorkOrderId, "DERP-2026-000002", SerializedItemStatus.InProduction));
    }

    [Fact]
    public async Task CreateAsync_WithBlankSerialNumber_ThrowsValidationException()
    {
        var (service, workOrder, _) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(workOrder.WorkOrderId, "   ", SerializedItemStatus.InProduction));
    }

    [Fact]
    public void SuggestNextSerialNumber_IncrementsPastHighestExisting()
    {
        var (service, _, _) = CreateService();
        var year = DateTime.UtcNow.Year;
        var existing = new List<SerializedItem>
        {
            new() { SerialNumber = $"DERP-{year}-001005" },
            new() { SerialNumber = $"DERP-{year}-001010" },
        };

        var suggestion = service.SuggestNextSerialNumber(existing);

        Assert.Equal($"DERP-{year}-001011", suggestion);
    }
}
