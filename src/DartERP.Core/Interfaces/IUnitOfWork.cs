namespace DartERP.Core.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
