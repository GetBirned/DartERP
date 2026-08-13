using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class QualityInspectionService
{
    private readonly IQualityInspectionRepository _repository;
    private readonly ISerializedItemRepository _serializedItemRepository;

    public QualityInspectionService(IQualityInspectionRepository repository, ISerializedItemRepository serializedItemRepository)
    {
        _repository = repository;
        _serializedItemRepository = serializedItemRepository;
    }

    public Task<List<QualityInspection>> GetAllWithDetailsAsync() => _repository.GetAllWithDetailsAsync();

    public Task<List<QualityInspection>> GetPendingAsync() => _repository.GetPendingAsync();

    public Task<int> GetPendingCountAsync() => _repository.GetPendingCountAsync();

    public async Task<QualityInspection> CreateAsync(int serializedItemId, string inspector, QualityResult result, string notes)
    {
        Validate(serializedItemId, inspector);

        var inspection = new QualityInspection
        {
            SerializedItemId = serializedItemId,
            InspectionDate = DateTime.UtcNow,
            Inspector = inspector.Trim(),
            Result = result,
            Notes = notes,
        };

        await _repository.AddAsync(inspection);
        return inspection;
    }

    public async Task UpdateAsync(QualityInspection existing, string inspector, QualityResult result, string notes)
    {
        Validate(existing.SerializedItemId, inspector);

        existing.Inspector = inspector.Trim();
        existing.Result = result;
        existing.Notes = notes;

        await _repository.UpdateAsync(existing);
    }

    private static void Validate(int serializedItemId, string inspector)
    {
        if (serializedItemId <= 0)
            throw new ValidationException("Select a serialized item before saving.");

        if (string.IsNullOrWhiteSpace(inspector))
            throw new ValidationException("Inspector name is required.");
    }
}
