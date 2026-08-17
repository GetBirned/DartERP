using DartERP.Application;
using DartERP.Application.Services;
using DartERP.Core.Interfaces;
using DartERP.Infrastructure.Data;
using DartERP.Infrastructure.Repositories;
using DartERP.Infrastructure.Seed;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
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
        Theme.CurrentMode = AppPreferences.Load().Theme;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .Build();

        // My Syncfusion license key lives in the gitignored appsettings.local.json,
        // never in source control. Registering it is optional at runtime too —
        // without it the controls just fall back to an unlicensed trial dialog
        // instead of the app failing to start.
        var syncfusionLicenseKey = configuration["Syncfusion:LicenseKey"];
        if (!string.IsNullOrWhiteSpace(syncfusionLicenseKey))
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);

        using var serviceProvider = services.BuildServiceProvider();

        EnsureDatabaseReady(serviceProvider);

        var currentUserContext = serviceProvider.Resolve<CurrentUserContext>();

        // Loops back to the login screen after a Log Out (MainForm.LoggedOut)
        // rather than exiting the process — a plain window close (X button)
        // has LoggedOut still false, so that falls through and ends the app
        // like normal.
        while (true)
        {
            using var loginForm = serviceProvider.Resolve<LoginForm>();
            if (loginForm.ShowDialog() != DialogResult.OK)
                break;

            using var mainForm = serviceProvider.Resolve<MainForm>();
            System.Windows.Forms.Application.Run(mainForm);

            if (!mainForm.LoggedOut)
                break;

            currentUserContext.SignOut();
        }
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
        services.AddSingleton<IDispositionRepository, DispositionRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();

        services.AddSingleton<CustomerService>();
        services.AddSingleton<VendorService>();
        services.AddSingleton<ProductService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<PurchaseOrderService>();
        services.AddSingleton<WorkOrderService>();
        services.AddSingleton<SerializedItemService>();
        services.AddSingleton<QualityInspectionService>();
        services.AddSingleton<DispositionService>();
        services.AddSingleton<DashboardService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<CurrentUserContext>();

        // Transient, not Singleton, on purpose — logging out needs a brand
        // new MainForm next time (fresh header identity for whoever signs
        // in next) and a fresh LoginForm each pass through the loop above.
        // Everything they depend on (the services above) stays Singleton;
        // only the forms themselves get re-created.
        services.AddTransient<LoginForm>();
        services.AddTransient<MainForm>();
    }

    /// <summary>
    /// Applies pending migrations and seeds demo data on first run so the
    /// app always launches into a populated, demo-ready state.
    /// </summary>
    private static void EnsureDatabaseReady(IServiceProvider serviceProvider)
    {
        var contextFactory = serviceProvider.Resolve<IDbContextFactory<DartErpDbContext>>();
        using var context = contextFactory.CreateDbContext();

        context.Database.Migrate();
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
    }
}
