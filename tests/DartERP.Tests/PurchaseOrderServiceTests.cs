using DartERP.Application;
using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.DTOs;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class PurchaseOrderServiceTests
{
    private static (PurchaseOrderService Service, FakeVendorRepository Vendors, FakePurchaseOrderRepository Orders) CreateService()
    {
        var vendors = new FakeVendorRepository(
        [
            new Vendor { CompanyName = "Active Vendor", VendorNumber = "VEND-2001", IsActive = true },
            new Vendor { CompanyName = "Inactive Vendor", VendorNumber = "VEND-2002", IsActive = false },
        ]);
        var orders = new FakePurchaseOrderRepository();
        var currentUser = new CurrentUserContext();
        currentUser.SignIn(new User { UserId = 1, DisplayName = "Alex Reyes" });
        return (new PurchaseOrderService(orders, vendors, currentUser), vendors, orders);
    }

    [Fact]
    public async Task CreateAsync_WithNoVendorSelected_ThrowsValidationException()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(0, null, string.Empty, PurchaseOrderStatus.Draft, []));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveVendor_ThrowsValidationException()
    {
        var (service, vendors, _) = CreateService();
        var inactiveVendor = (await vendors.GetAllAsync()).First(v => !v.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(inactiveVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []));
    }

    [Fact]
    public async Task CreateAsync_SubmittedWithNoLines_ThrowsValidationException()
    {
        var (service, vendors, _) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Submitted, []));
    }

    [Fact]
    public async Task CreateAsync_DraftWithNoLines_Succeeds()
    {
        var (service, vendors, _) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);

        var po = await service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []);

        Assert.Equal(0m, po.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_WithZeroQuantityLine_ThrowsValidationException()
    {
        var (service, vendors, _) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var lines = new List<PurchaseOrderLineInput> { new() { ProductId = 1, Quantity = 0, UnitCost = 10m } };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, lines));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeUnitCost_ThrowsValidationException()
    {
        var (service, vendors, _) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var lines = new List<PurchaseOrderLineInput> { new() { ProductId = 1, Quantity = 5, UnitCost = -1m } };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, lines));
    }

    [Fact]
    public async Task CreateAsync_ComputesLineAndOrderTotals()
    {
        var (service, vendors, _) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var lines = new List<PurchaseOrderLineInput>
        {
            new() { ProductId = 1, Quantity = 3, UnitCost = 5.75m },
            new() { ProductId = 2, Quantity = 1, UnitCost = 5.75m },
        };

        var po = await service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, lines);

        Assert.Equal(17.25m, po.Lines.First().LineTotal);
        Assert.Equal(23.00m, po.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_LogsInitialStatusHistoryEntry()
    {
        var (service, vendors, orders) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);

        var po = await service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []);
        var history = await orders.GetStatusHistoryAsync(po.PurchaseOrderId);

        var entry = Assert.Single(history);
        Assert.Null(entry.FromStatus);
        Assert.Equal(PurchaseOrderStatus.Draft, entry.ToStatus);
    }

    [Fact]
    public async Task UpdateAsync_WithStatusChange_LogsHistoryEntry()
    {
        var (service, vendors, orders) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var po = await service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []);
        var lines = new List<PurchaseOrderLineInput> { new() { ProductId = 1, Quantity = 5, UnitCost = 10m } };

        await service.UpdateAsync(po.PurchaseOrderId, activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Submitted, lines);
        var history = await orders.GetStatusHistoryAsync(po.PurchaseOrderId);

        Assert.Equal(2, history.Count);
        var latest = history.First(h => h.FromStatus is not null);
        Assert.Equal(PurchaseOrderStatus.Draft, latest.FromStatus);
        Assert.Equal(PurchaseOrderStatus.Submitted, latest.ToStatus);
    }

    [Fact]
    public async Task UpdateAsync_WithoutStatusChange_DoesNotLogEntry()
    {
        var (service, vendors, orders) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var po = await service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []);

        await service.UpdateAsync(po.PurchaseOrderId, activeVendor.VendorId, null, "updated notes", PurchaseOrderStatus.Draft, []);
        var history = await orders.GetStatusHistoryAsync(po.PurchaseOrderId);

        Assert.Single(history);
    }
}
