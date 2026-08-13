using DartERP.Core.Interfaces;
using DartERP.Infrastructure.Data;

namespace DartERP.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DartErpDbContext _context;

    public UnitOfWork(DartErpDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
