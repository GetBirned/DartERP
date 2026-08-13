using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public record InventorySummary(
    decimal TotalInventoryValue,
    int ActiveProductCount,
    int SerializedProductCount,
    int BelowReorderCount);

/// <summary>
/// Read-only inventory views over the product catalog: value, low-stock,
/// and serialized/non-serialized mix for the Inventory screen.
/// </summary>
public class InventoryService
{
    private readonly IProductRepository _repository;

    public InventoryService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<InventorySummary> GetSummaryAsync()
    {
        var active = await _repository.GetActiveAsync();
        var totalValue = await _repository.GetTotalInventoryValueAsync();
        var belowReorder = await _repository.GetBelowReorderLevelAsync();

        return new InventorySummary(
            TotalInventoryValue: totalValue,
            ActiveProductCount: active.Count,
            SerializedProductCount: active.Count(p => p.IsSerialized),
            BelowReorderCount: belowReorder.Count);
    }

    public Task<List<Product>> GetActiveProductsAsync() => _repository.GetActiveAsync();

    public Task<List<Product>> GetBelowReorderLevelAsync() => _repository.GetBelowReorderLevelAsync();
}
