using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DartERP.Infrastructure.Data;

/// <summary>
/// Lets `dotnet ef` construct the context at design time without needing
/// the WinForms host's DI container wired up.
/// </summary>
public class DartErpDbContextFactory : IDesignTimeDbContextFactory<DartErpDbContext>
{
    public DartErpDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DartErpDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=DartERP;Trusted_Connection=True;TrustServerCertificate=True;");

        return new DartErpDbContext(optionsBuilder.Options);
    }
}
