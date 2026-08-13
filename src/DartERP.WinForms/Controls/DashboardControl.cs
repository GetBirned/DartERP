using DartERP.Application.Services;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class DashboardControl : UserControl
{
    private readonly DashboardService _service;
    private readonly FlowLayoutPanel _kpiPanel;
    private readonly TableLayoutPanel _cardsPanel;
    private readonly DashboardListCard _recentPurchaseOrdersCard;
    private readonly DashboardListCard _belowReorderCard;
    private readonly DashboardListCard _dueSoonCard;
    private readonly DashboardListCard _pendingInspectionsCard;

    public DashboardControl(DashboardService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        _kpiPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 232,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };

        _cardsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
        };
        _cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _recentPurchaseOrdersCard = new DashboardListCard("Recent Purchase Orders", "No purchase orders yet.");
        _belowReorderCard = new DashboardListCard("Products Below Reorder Level", "All products are above their reorder level.");
        _dueSoonCard = new DashboardListCard("Work Orders Due Soon", "Nothing due in the next 7 days.");
        _pendingInspectionsCard = new DashboardListCard("Pending Quality Inspections", "No inspections awaiting a result.");

        _cardsPanel.Controls.Add(_recentPurchaseOrdersCard, 0, 0);
        _cardsPanel.Controls.Add(_belowReorderCard, 1, 0);
        _cardsPanel.Controls.Add(_dueSoonCard, 0, 1);
        _cardsPanel.Controls.Add(_pendingInspectionsCard, 1, 1);

        Controls.Add(_cardsPanel);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16 });
        Controls.Add(_kpiPanel);

        Load += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var summary = await _service.GetSummaryAsync();

        _kpiPanel.Controls.Clear();
        _kpiPanel.Controls.Add(new KpiCard("Active Customers", summary.ActiveCustomers.ToString()) { AccentColor = Theme.AccentPrimary });
        _kpiPanel.Controls.Add(new KpiCard("Active Vendors", summary.ActiveVendors.ToString()) { AccentColor = Theme.AccentPrimary });
        _kpiPanel.Controls.Add(new KpiCard("Open Purchase Orders", summary.OpenPurchaseOrders.ToString()) { AccentColor = Theme.WarningAmber });
        _kpiPanel.Controls.Add(new KpiCard("Open Work Orders", summary.OpenWorkOrders.ToString()) { AccentColor = Theme.WarningAmber });
        _kpiPanel.Controls.Add(new KpiCard("Inventory Value", summary.InventoryValue.ToString("C0")) { AccentColor = Theme.SuccessGreen });
        _kpiPanel.Controls.Add(new KpiCard("Units In Production", summary.UnitsInProduction.ToString()) { AccentColor = Theme.SuccessGreen });

        _recentPurchaseOrdersCard.SetRows(summary.RecentPurchaseOrders
            .Select(po => new DashboardListRow(
                $"{po.PurchaseOrderNumber}  {po.Vendor?.CompanyName}",
                po.TotalAmount.ToString("C0")))
            .ToList());

        _belowReorderCard.SetRows(summary.ProductsBelowReorder
            .Select(p => new DashboardListRow(
                $"{p.SKU}  {p.ProductName}",
                $"{p.QuantityOnHand} on hand",
                Theme.WarningAmber))
            .ToList());

        _dueSoonCard.SetRows(summary.WorkOrdersDueSoon
            .Select(w => new DashboardListRow(
                $"{w.WorkOrderNumber}  {w.Product?.ProductName}",
                w.DueDate.ToString("MM/dd/yyyy")))
            .ToList());

        _pendingInspectionsCard.SetRows(summary.PendingInspections
            .Select(q => new DashboardListRow(
                $"{q.SerializedItem?.SerialNumber}  {q.SerializedItem?.Product?.ProductName}",
                "Pending",
                Theme.WarningAmber))
            .ToList());
    }
}
