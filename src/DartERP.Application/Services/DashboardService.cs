using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public record DashboardSummary(
    int ActiveCustomers,
    int ActiveVendors,
    int OpenPurchaseOrders,
    int OpenWorkOrders,
    decimal InventoryValue,
    int UnitsInProduction,
    List<PurchaseOrder> RecentPurchaseOrders,
    List<Product> ProductsBelowReorder,
    List<WorkOrder> WorkOrdersDueSoon,
    List<QualityInspection> PendingInspections);

/// <summary>
/// Aggregates KPI and attention-needed data for the dashboard. Every
/// underlying query runs against its own short-lived DbContext (via the
/// repositories' IDbContextFactory), so they're safe to fire concurrently.
/// </summary>
public class DashboardService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IQualityInspectionRepository _qualityInspectionRepository;

    public DashboardService(
        ICustomerRepository customerRepository,
        IVendorRepository vendorRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IWorkOrderRepository workOrderRepository,
        IProductRepository productRepository,
        IQualityInspectionRepository qualityInspectionRepository)
    {
        _customerRepository = customerRepository;
        _vendorRepository = vendorRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _workOrderRepository = workOrderRepository;
        _productRepository = productRepository;
        _qualityInspectionRepository = qualityInspectionRepository;
    }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var activeCustomersTask = _customerRepository.GetActiveCountAsync();
        var activeVendorsTask = _vendorRepository.GetActiveCountAsync();
        var openPurchaseOrdersTask = _purchaseOrderRepository.GetOpenCountAsync();
        var openWorkOrdersTask = _workOrderRepository.GetOpenCountAsync();
        var inventoryValueTask = _productRepository.GetTotalInventoryValueAsync();
        var unitsInProductionTask = _workOrderRepository.GetUnitsInProductionAsync();
        var recentPurchaseOrdersTask = _purchaseOrderRepository.GetRecentAsync(5);
        var belowReorderTask = _productRepository.GetBelowReorderLevelAsync();
        var dueSoonTask = _workOrderRepository.GetDueSoonAsync(7);
        var pendingInspectionsTask = _qualityInspectionRepository.GetPendingAsync();

        await Task.WhenAll(
            activeCustomersTask, activeVendorsTask, openPurchaseOrdersTask, openWorkOrdersTask,
            inventoryValueTask, unitsInProductionTask, recentPurchaseOrdersTask, belowReorderTask,
            dueSoonTask, pendingInspectionsTask);

        return new DashboardSummary(
            activeCustomersTask.Result,
            activeVendorsTask.Result,
            openPurchaseOrdersTask.Result,
            openWorkOrdersTask.Result,
            inventoryValueTask.Result,
            unitsInProductionTask.Result,
            recentPurchaseOrdersTask.Result,
            belowReorderTask.Result,
            dueSoonTask.Result,
            pendingInspectionsTask.Result);
    }
}
