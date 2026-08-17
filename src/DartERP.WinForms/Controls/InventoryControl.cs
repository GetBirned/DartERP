using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Controls;

public class InventoryControl : UserControl
{
    private record InventoryRow(string SKU, string ProductName, string Category, string QuantityOnHand, string ReorderLevel, string Value, string Serialized, string StockStatus, bool LowStock);

    private readonly InventoryService _service;
    private readonly FlowLayoutPanel _kpiPanel;
    private readonly SfDataGrid _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public InventoryControl(InventoryService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        _kpiPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 116,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        var sectionLabel = new LetterSpacedLabel
        {
            Text = "Inventory by Product",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "Inventory.csv");

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 40 };
        titleBar.Controls.Add(sectionLabel);
        titleBar.Controls.Add(exportButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
        BuildColumns();

        _gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground };
        _gridHost.Controls.Add(_grid);

        Controls.Add(_gridHost);
        Controls.Add(titleBar);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16 });
        Controls.Add(_kpiPanel);

        Load += async (_, _) => await RefreshAsync();
    }

    private void BuildColumns()
    {
        _grid.Columns.Add(new GridTextColumn { MappingName = "SKU", HeaderText = "SKU", Width = 90 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "ProductName", HeaderText = "Product", Width = 170 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Category", HeaderText = "Category", Width = 110 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "QuantityOnHand", HeaderText = "Qty On Hand", Width = 90 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "ReorderLevel", HeaderText = "Reorder Level", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Value", HeaderText = "Inventory Value", Width = 110 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Serialized", HeaderText = "Serialized", Width = 80 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "StockStatus", HeaderText = "Stock Status", Width = 100 });

        _grid.QueryCellStyle += (_, e) =>
        {
            if (e.Column.MappingName != "StockStatus" || e.DataRow.RowData is not InventoryRow row)
                return;

            e.Style.TextColor = row.LowStock ? Theme.WarningAmber : Theme.SuccessGreen;
            e.Style.Font.Bold = true;
        };

        // Tints the whole row, not just the StockStatus cell — same effect
        // the old CellFormatting had by setting CellStyle.BackColor on every
        // column. Runs after StyleAsSfDataGrid's alternating-row handler, so
        // this BackColor wins for rows that are both alternating AND low stock.
        _grid.QueryRowStyle += (_, e) =>
        {
            if (e.RowData is InventoryRow { LowStock: true })
                e.Style.BackColor = Theme.WarningTint;
        };
    }

    private async Task RefreshAsync()
    {
        var summary = await _service.GetSummaryAsync();

        // Navigating away disposes this control while the query above is
        // still in flight — SfDataGrid throws on a DataSource assignment
        // after disposal (DataGridView never did), so this guard is load-bearing.
        if (IsDisposed)
            return;

        _kpiPanel.Controls.Clear();
        _kpiPanel.Controls.Add(new KpiCard("Inventory Value", summary.TotalInventoryValue.ToString("C0")) { AccentColor = Theme.AccentPrimary });
        _kpiPanel.Controls.Add(new KpiCard("Active Products", summary.ActiveProductCount.ToString()) { AccentColor = Theme.SuccessGreen });
        _kpiPanel.Controls.Add(new KpiCard("Serialized Products", summary.SerializedProductCount.ToString()) { AccentColor = Theme.AccentPrimary });
        _kpiPanel.Controls.Add(new KpiCard("Below Reorder Level", summary.BelowReorderCount.ToString())
        {
            AccentColor = summary.BelowReorderCount > 0 ? Theme.WarningAmber : Theme.SuccessGreen,
        });

        var products = await _service.GetActiveProductsAsync();

        if (IsDisposed)
            return;

        if (products.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No inventory to show", "Add products to start tracking inventory.");
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = products.OrderBy(p => p.QuantityOnHand - p.ReorderLevel).Select(p =>
            {
                var lowStock = p.QuantityOnHand <= p.ReorderLevel;
                return new InventoryRow(
                    p.SKU, p.ProductName, EnumDisplay.For(p.Category), p.QuantityOnHand.ToString(), p.ReorderLevel.ToString(),
                    (p.UnitCost * p.QuantityOnHand).ToString("C2"), p.IsSerialized ? "Yes" : "No",
                    lowStock ? "Below Reorder" : "OK", lowStock);
            }).ToList();
        }
    }
}
