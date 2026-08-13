using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.Tests.Fakes;
using Xunit;

namespace DartERP.Tests;

public class ProductServiceTests
{
    private static ProductService CreateService(FakeProductRepository? repository = null) =>
        new(repository ?? new FakeProductRepository());

    [Fact]
    public async Task CreateAsync_WithDuplicateSku_ThrowsValidationException()
    {
        var repository = new FakeProductRepository([
            new Product { SKU = "DERP-1001", ProductName = "Model Alpha", IsActive = true },
        ]);
        var service = CreateService(repository);

        var duplicate = new Product { SKU = "DERP-1001", ProductName = "Model Alpha Clone", UnitCost = 10m, SalePrice = 20m };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(duplicate));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeUnitCost_ThrowsValidationException()
    {
        var service = CreateService();
        var product = new Product { SKU = "DERP-9001", ProductName = "Test Product", UnitCost = -5m, SalePrice = 10m };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(product));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_Succeeds()
    {
        var service = CreateService();
        var product = new Product { SKU = "DERP-9002", ProductName = "Test Product", UnitCost = 10m, SalePrice = 20m, Category = ProductCategory.Component };

        var created = await service.CreateAsync(product);

        Assert.True(created.IsActive);
        Assert.True(created.ProductId > 0);
    }
}
