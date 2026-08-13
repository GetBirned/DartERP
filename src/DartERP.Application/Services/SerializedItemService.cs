using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class SerializedItemService
{
    private readonly ISerializedItemRepository _repository;
    private readonly IWorkOrderRepository _workOrderRepository;

    public SerializedItemService(ISerializedItemRepository repository, IWorkOrderRepository workOrderRepository)
    {
        _repository = repository;
        _workOrderRepository = workOrderRepository;
    }

    public Task<List<SerializedItem>> GetAllWithDetailsAsync() => _repository.GetAllWithDetailsAsync();

    public string SuggestNextSerialNumber(List<SerializedItem> existing)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"DERP-{year}-";

        var nextSeq = existing
            .Where(s => s.SerialNumber.StartsWith(prefix))
            .Select(s => int.TryParse(s.SerialNumber[prefix.Length..], out var seq) ? seq : 0)
            .DefaultIfEmpty(1000)
            .Max() + 1;

        return $"{prefix}{nextSeq:D6}";
    }

    public async Task<SerializedItem> CreateAsync(int workOrderId, string serialNumber, SerializedItemStatus status)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId)
            ?? throw new ValidationException("Select a work order before saving.");

        if (workOrder.Product is null || !workOrder.Product.IsSerialized)
            throw new ValidationException("The selected work order's product is not serialized.");

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ValidationException("Serial number is required.");

        if (await _repository.SerialNumberExistsAsync(serialNumber))
            throw new ValidationException($"Serial number '{serialNumber}' is already in use.");

        var item = new SerializedItem
        {
            SerialNumber = serialNumber.Trim(),
            ProductId = workOrder.ProductId,
            WorkOrderId = workOrderId,
            Status = status,
            CreatedDate = DateTime.UtcNow,
        };

        await _repository.AddAsync(item);
        return item;
    }

    public async Task UpdateStatusAsync(int serializedItemId, SerializedItemStatus status)
    {
        var item = await _repository.GetByIdAsync(serializedItemId)
            ?? throw new ValidationException("This serialized item no longer exists.");

        item.Status = status;
        await _repository.UpdateAsync(item);
    }
}
