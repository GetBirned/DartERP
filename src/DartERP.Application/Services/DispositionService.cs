using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class DispositionService
{
    private readonly IDispositionRepository _repository;
    private readonly ISerializedItemRepository _serializedItemRepository;

    public DispositionService(IDispositionRepository repository, ISerializedItemRepository serializedItemRepository)
    {
        _repository = repository;
        _serializedItemRepository = serializedItemRepository;
    }

    public Task<List<Disposition>> GetAllWithDetailsAsync() => _repository.GetAllWithDetailsAsync();

    public async Task<Disposition> CreateAsync(int serializedItemId, DateTime dispositionDate, DispositionType type, int? customerId, string notes)
    {
        var serializedItem = await _serializedItemRepository.GetByIdAsync(serializedItemId)
            ?? throw new ValidationException("Select a serialized item before saving.");

        if (RequiresCustomer(type) && customerId is null)
            throw new ValidationException($"A recipient is required for a {type} disposition.");

        var disposition = new Disposition
        {
            SerializedItemId = serializedItemId,
            DispositionDate = dispositionDate,
            Type = type,
            CustomerId = RequiresCustomer(type) || type == DispositionType.Returned ? customerId : null,
            Notes = notes,
        };

        await _repository.AddAsync(disposition);

        // The bound-book entry and the item's current inventory status are
        // two views of the same fact, so recording a disposition also
        // moves the item's status to match — otherwise Serialized Inventory
        // would keep showing it as "In Stock" after it's been sold.
        serializedItem.Status = type switch
        {
            DispositionType.Sold => SerializedItemStatus.Shipped,
            DispositionType.Transferred => SerializedItemStatus.Shipped,
            DispositionType.Destroyed => SerializedItemStatus.Scrapped,
            DispositionType.Returned => SerializedItemStatus.InStock,
            _ => serializedItem.Status,
        };
        await _serializedItemRepository.UpdateAsync(serializedItem);

        return disposition;
    }

    public static bool RequiresCustomer(DispositionType type) =>
        type is DispositionType.Sold or DispositionType.Transferred;
}
