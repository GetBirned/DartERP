using DartERP.Application.Services;
using DartERP.Core.Interfaces;
using DartERP.Infrastructure.Data;
using DartERP.Infrastructure.Repositories;
using DartERP.Infrastructure.Seed;
using DartERP.WinForms.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartERP.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);

        using var serviceProvider = services.BuildServiceProvider();

        EnsureDatabaseReady(serviceProvider);

        var mainForm = serviceProvider.GetRequiredService<MainForm>();
        System.Windows.Forms.Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DartErp")
            ?? throw new InvalidOperationException("Missing 'DartErp' connection string in appsettings.json.");

        services.AddDbContextFactory<DartErpDbContext>(options => options.UseSqlServer(connectionString));

        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<IVendorRepository, VendorRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddSingleton<IWorkOrderRepository, WorkOrderRepository>();
        services.AddSingleton<ISerializedItemRepository, SerializedItemRepository>();
        services.AddSingleton<IQualityInspectionRepository, QualityInspectionRepository>();

        services.AddSingleton<CustomerService>();
        services.AddSingleton<VendorService>();

        services.AddSingleton<MainForm>();
    }

    /// <summary>
    /// Applies pending migrations and seeds demo data on first run so the
    /// app always launches into a populated, demo-ready state.
    /// </summary>
    private static void EnsureDatabaseReady(IServiceProvider serviceProvider)
    {
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<DartErpDbContext>>();
        using var context = contextFactory.CreateDbContext();

        context.Database.Migrate();
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
    }
}
