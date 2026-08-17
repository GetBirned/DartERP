using DartERP.Application.Services;
using DartERP.Core.Enums;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class DashboardControl : UserControl
{
    private readonly DashboardService _service;
    private readonly TableLayoutPanel _kpiPanel;
    private readonly TableLayoutPanel _cardsPanel;
    private readonly DashboardListCard _recentPurchaseOrdersCard;
    private readonly DashboardListCard _belowReorderCard;
    private readonly DashboardListCard _dueSoonCard;
    private readonly DashboardListCard _pendingInspectionsCard;
    private readonly SfPieChartCard _purchaseOrdersByStatusChart;
    private readonly SfBarChartCard _inventoryValueByCategoryChart;

    public DashboardControl(DashboardService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        // A 3x2 grid, not a wrapping flow panel — six cards divides evenly
        // into 3 columns, so every row is always fully populated instead of
        // a FlowLayoutPanel leaving a half-empty trailing row whenever the
        // window is wide enough to fit 4 across but there are only 2 left.
        _kpiPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 232,
            ColumnCount = 3,
            RowCount = 2,
        };
        _kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        _kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        _kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        _kpiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _kpiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        // Charts get noticeably more width than the two list-card columns —
        // a donut legend and a bar chart's value-axis labels both need real
        // horizontal room, which a plain three-way even split never gave them.
        _cardsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
        };
        _cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        _cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        _cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        _cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _recentPurchaseOrdersCard = new DashboardListCard("Recent Purchase Orders", "No purchase orders yet.");
        _belowReorderCard = new DashboardListCard("Products Below Reorder Level", "All products are above their reorder level.");
        _dueSoonCard = new DashboardListCard("Work Orders Due Soon", "Nothing due in the next 7 days.");
        _pendingInspectionsCard = new DashboardListCard("Pending Quality Inspections", "No inspections awaiting a result.");
        _purchaseOrdersByStatusChart = new SfPieChartCard("Purchase Orders by Status");
        _inventoryValueByCategoryChart = new SfBarChartCard("Inventory Value by Category", "$#,##0,K");

        _cardsPanel.Controls.Add(_recentPurchaseOrdersCard, 0, 0);
        _cardsPanel.Controls.Add(_belowReorderCard, 1, 0);
        _cardsPanel.Controls.Add(_purchaseOrdersByStatusChart, 2, 0);
        _cardsPanel.Controls.Add(_dueSoonCard, 0, 1);
        _cardsPanel.Controls.Add(_pendingInspectionsCard, 1, 1);
        _cardsPanel.Controls.Add(_inventoryValueByCategoryChart, 2, 1);

        Controls.Add(_cardsPanel);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16 });
        Controls.Add(_kpiPanel);

        Load += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var summary = await _service.GetSummaryAsync();

        // Navigating away disposes this control while the query above is
        // still in flight — SfDataGrid/ChartControl throw on a disposed
        // access (DataGridView and the old GDI+ charts never did), so this
        // guard is load-bearing.
        if (IsDisposed)
            return;

        _kpiPanel.Controls.Clear();
        AddKpiCard(0, 0, "Active Customers", summary.ActiveCustomers.ToString(), Theme.AccentPrimary);
        AddKpiCard(1, 0, "Active Vendors", summary.ActiveVendors.ToString(), Theme.AccentPrimary);
        AddKpiCard(2, 0, "Open Purchase Orders", summary.OpenPurchaseOrders.ToString(), Theme.WarningAmber);
        AddKpiCard(0, 1, "Open Work Orders", summary.OpenWorkOrders.ToString(), Theme.WarningAmber);
        AddKpiCard(1, 1, "Inventory Value", summary.InventoryValue.ToString("C0"), Theme.SuccessGreen);
        AddKpiCard(2, 1, "Units In Production", summary.UnitsInProduction.ToString(), Theme.SuccessGreen);

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

        _purchaseOrdersByStatusChart.SetData(Enum.GetValues<PurchaseOrderStatus>()
            .Select(status => new PieSlice(
                EnumDisplay.For(status),
                summary.PurchaseOrdersByStatus.GetValueOrDefault(status),
                StatusColors.For(status)))
            .ToList());

        _inventoryValueByCategoryChart.SetData(Enum.GetValues<ProductCategory>()
            .Select(category => new BarSegment(
                EnumDisplay.For(category),
                summary.InventoryValueByCategory.GetValueOrDefault(category)))
            .Where(bar => bar.Value > 0)
            .ToList());
    }

    private void AddKpiCard(int column, int row, string title, string value, Color accentColor)
    {
        var card = new KpiCard(title, value) { AccentColor = accentColor, Dock = DockStyle.Fill };
        _kpiPanel.Controls.Add(card, column, row);
    }
}
