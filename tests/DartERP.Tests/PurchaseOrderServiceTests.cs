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
    private static (PurchaseOrderService Service, FakeVendorRepository Vendors) CreateService()
    {
        var vendors = new FakeVendorRepository(
        [
            new Vendor { CompanyName = "Active Vendor", VendorNumber = "VEND-2001", IsActive = true },
            new Vendor { CompanyName = "Inactive Vendor", VendorNumber = "VEND-2002", IsActive = false },
        ]);
        var orders = new FakePurchaseOrderRepository();
        return (new PurchaseOrderService(orders, vendors), vendors);
    }

    [Fact]
    public async Task CreateAsync_WithNoVendorSelected_ThrowsValidationException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(0, null, string.Empty, PurchaseOrderStatus.Draft, []));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveVendor_ThrowsValidationException()
    {
        var (service, vendors) = CreateService();
        var inactiveVendor = (await vendors.GetAllAsync()).First(v => !v.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(inactiveVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []));
    }

    [Fact]
    public async Task CreateAsync_SubmittedWithNoLines_ThrowsValidationException()
    {
        var (service, vendors) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Submitted, []));
    }

    [Fact]
    public async Task CreateAsync_DraftWithNoLines_Succeeds()
    {
        var (service, vendors) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);

        var po = await service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, []);

        Assert.Equal(0m, po.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_WithZeroQuantityLine_ThrowsValidationException()
    {
        var (service, vendors) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var lines = new List<PurchaseOrderLineInput> { new() { ProductId = 1, Quantity = 0, UnitCost = 10m } };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, lines));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeUnitCost_ThrowsValidationException()
    {
        var (service, vendors) = CreateService();
        var activeVendor = (await vendors.GetAllAsync()).First(v => v.IsActive);
        var lines = new List<PurchaseOrderLineInput> { new() { ProductId = 1, Quantity = 5, UnitCost = -1m } };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(activeVendor.VendorId, null, string.Empty, PurchaseOrderStatus.Draft, lines));
    }

    [Fact]
    public async Task CreateAsync_ComputesLineAndOrderTotals()
    {
        var (service, vendors) = CreateService();
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
}
