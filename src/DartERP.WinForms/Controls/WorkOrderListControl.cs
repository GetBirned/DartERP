using DartERP.Application.Services;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Controls;

public class WorkOrderListControl : UserControl
{
    private const string AllStatusesOption = "All Statuses";

    private record WorkOrderRow(WorkOrder Source, string WorkOrderNumber, string Product, string Quantity, string StartDate, string DueDate, string Status, Color StatusColor);

    private readonly WorkOrderService _service;
    private readonly ProductService _productService;
    private readonly ComboBox _statusFilter;
    private readonly SfDataGrid _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;
    private List<WorkOrder> _allOrders = [];

    public WorkOrderListControl(WorkOrderService service, ProductService productService)
    {
        _service = service;
        _productService = productService;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Work Order", Width = 160, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync(null);

        _statusFilter = new ComboBox { Dock = DockStyle.Right, Width = 160 }.StyleAsInput();
        var options = new List<string> { AllStatusesOption };
        options.AddRange(Enum.GetValues<WorkOrderStatus>().Select(v => EnumDisplay.For(v)));
        _statusFilter.DataSource = options;
        _statusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var spacer = new Panel { Dock = DockStyle.Fill };

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "WorkOrders.csv");

        toolbar.Controls.Add(spacer);
        toolbar.Controls.Add(_statusFilter);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.DataRow.RowData is WorkOrderRow row)
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
        _grid.Columns.Add(new GridTextColumn { MappingName = "WorkOrderNumber", HeaderText = "WO #", Width = 90 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Product", HeaderText = "Product", Width = 180 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Quantity", HeaderText = "Qty", Width = 60 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "StartDate", HeaderText = "Start Date", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "DueDate", HeaderText = "Due Date", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Status", HeaderText = "Status", Width = 110 });

        _grid.QueryCellStyle += (_, e) =>
        {
            if (e.Column.MappingName != "Status" || e.DataRow.RowData is not WorkOrderRow row)
                return;

            e.Style.TextColor = row.StatusColor;
            e.Style.Font.Bold = true;
        };
    }

    private async Task RefreshAsync()
    {
        _allOrders = await _service.GetAllWithProductAsync();

        // Navigating away disposes this control while the query above is
        // still in flight — SfDataGrid throws on a DataSource assignment
        // after disposal (DataGridView never did), so this guard is load-bearing.
        if (IsDisposed)
            return;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selected = _statusFilter.SelectedItem as string ?? AllStatusesOption;
        var filtered = selected == AllStatusesOption
            ? _allOrders
            : _allOrders.Where(w => EnumDisplay.For(w.Status) == selected).ToList();

        if (filtered.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No work orders found", string.Empty);
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
            _emptyState.Subtitle = selected == AllStatusesOption
                ? "Create your first work order to get started."
                : "No work orders currently have this status.";
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = filtered.Select(wo => new WorkOrderRow(
                wo, wo.WorkOrderNumber, wo.Product?.ProductName ?? string.Empty, wo.Quantity.ToString(),
                wo.StartDate.ToString("MM/dd/yyyy"), wo.DueDate.ToString("MM/dd/yyyy"),
                EnumDisplay.For(wo.Status), StatusColors.For(wo.Status))).ToList();
        }
    }

    private async Task OpenEditorAsync(WorkOrder? existing)
    {
        var activeProducts = await _productService.GetActiveAsync();

        if (activeProducts.Count == 0)
        {
            MessageBox.Show(FindForm(), "Add at least one active product before creating a work order.",
                "No products available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new WorkOrderEditForm(_service, activeProducts, existing);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
