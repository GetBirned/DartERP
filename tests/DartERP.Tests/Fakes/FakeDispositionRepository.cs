using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Tests.Fakes;

public class FakeDispositionRepository : IDispositionRepository
{
    private readonly List<Disposition> _dispositions = [];
    private int _nextId = 1;

    public Task<Disposition?> GetByIdAsync(int id) => Task.FromResult(_dispositions.FirstOrDefault(d => d.DispositionId == id));

    public Task<List<Disposition>> GetAllAsync() => Task.FromResult(_dispositions.ToList());

    public Task AddAsync(Disposition entity)
    {
        entity.DispositionId = _nextId++;
        _dispositions.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Disposition entity) => Task.CompletedTask;

    public Task<List<Disposition>> GetAllWithDetailsAsync() => Task.FromResult(_dispositions.ToList());

    public Task<List<Disposition>> GetForSerializedItemAsync(int serializedItemId) =>
        Task.FromResult(_dispositions.Where(d => d.SerializedItemId == serializedItemId).ToList());
}
