using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Controls;

public class PurchaseOrderListControl : UserControl
{
    private record PurchaseOrderRow(PurchaseOrder Source, string PurchaseOrderNumber, string Vendor, string OrderDate, string ExpectedDate, string Status, Color StatusColor, string TotalAmount);

    private readonly PurchaseOrderService _service;
    private readonly VendorService _vendorService;
    private readonly ProductService _productService;
    private readonly SfDataGrid _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public PurchaseOrderListControl(PurchaseOrderService service, VendorService vendorService, ProductService productService)
    {
        _service = service;
        _vendorService = vendorService;
        _productService = productService;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Purchase Order", Width = 180, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync(null);

        var titleLabel = new Label
        {
            Text = "All purchase orders, most recent first",
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "PurchaseOrders.csv");

        toolbar.Controls.Add(titleLabel);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.DataRow.RowData is PurchaseOrderRow row)
                await OpenEditorAsync(row.Source);
        };

        _gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground };
        _gridHost.Controls.Add(_grid);

        Controls.Add(_gridHost);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        Controls.Add(toolbar);

        Load += async (_, _) => await RefreshAsync();
    }

    private void BuildColumns()
    {
        _grid.Columns.Add(new GridTextColumn { MappingName = "PurchaseOrderNumber", HeaderText = "PO #", Width = 90 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Vendor", HeaderText = "Vendor", Width = 190 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "OrderDate", HeaderText = "Order Date", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "ExpectedDate", HeaderText = "Expected Date", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Status", HeaderText = "Status", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "TotalAmount", HeaderText = "Total", Width = 100 });

        _grid.QueryCellStyle += (_, e) =>
        {
            if (e.Column.MappingName != "Status" || e.DataRow.RowData is not PurchaseOrderRow row)
                return;

            e.Style.TextColor = row.StatusColor;
            e.Style.Font.Bold = true;
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.GetAllWithVendorAsync();

        // Navigating away disposes this control while the query above is
        // still in flight — SfDataGrid throws on a DataSource assignment
        // after disposal (DataGridView never did), so this guard is load-bearing.
        if (IsDisposed)
            return;

        if (results.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No purchase orders yet", "Create your first purchase order to get started.");
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = results.Select(po => new PurchaseOrderRow(
                po, po.PurchaseOrderNumber, po.Vendor?.CompanyName ?? string.Empty, po.OrderDate.ToString("MM/dd/yyyy"),
                po.ExpectedDate?.ToString("MM/dd/yyyy") ?? "-", EnumDisplay.For(po.Status), StatusColors.For(po.Status),
                po.TotalAmount.ToString("C2"))).ToList();
        }
    }

    private async Task OpenEditorAsync(PurchaseOrder? summary)
    {
        var activeVendors = await _vendorService.GetActiveAsync();
        var activeProducts = await _productService.GetActiveAsync();

        if (activeVendors.Count == 0)
        {
            MessageBox.Show(FindForm(), "Add at least one active vendor before creating a purchase order.",
                "No vendors available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PurchaseOrder? existing = summary is null ? null : await _service.GetWithLinesAsync(summary.PurchaseOrderId);

        using var form = new PurchaseOrderEditForm(_service, activeVendors, activeProducts, existing);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
