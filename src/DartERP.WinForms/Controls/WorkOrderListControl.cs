using DartERP.Application.Services;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class WorkOrderListControl : UserControl
{
    private const string AllStatusesOption = "All Statuses";

    private readonly WorkOrderService _service;
    private readonly ProductService _productService;
    private readonly ComboBox _statusFilter;
    private readonly DataGridView _grid;
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

        toolbar.Controls.Add(spacer);
        toolbar.Controls.Add(_statusFilter);
        toolbar.Controls.Add(newButton);

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenEditorAsync((WorkOrder)_grid.Rows[e.RowIndex].DataBoundItem);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkOrderNumber", HeaderText = "WO #", DataPropertyName = "WorkOrderNumber", FillWeight = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", FillWeight = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Qty", DataPropertyName = "Quantity", FillWeight = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartDate", HeaderText = "Start Date", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DueDate", HeaderText = "Due Date", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 110 });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var wo = (WorkOrder)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "Product":
                    e.Value = wo.Product?.ProductName ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "StartDate":
                    e.Value = wo.StartDate.ToString("MM/dd/yyyy");
                    e.FormattingApplied = true;
                    break;
                case "DueDate":
                    e.Value = wo.DueDate.ToString("MM/dd/yyyy");
                    e.FormattingApplied = true;
                    break;
                case "Status" when e.CellStyle is not null:
                    e.Value = EnumDisplay.For(wo.Status);
                    e.CellStyle.ForeColor = StatusColors.For(wo.Status);
                    e.CellStyle.Font = Theme.FontBodyBold;
                    e.FormattingApplied = true;
                    break;
            }
        };
    }

    private async Task RefreshAsync()
    {
        _allOrders = await _service.GetAllWithProductAsync();
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
            _grid.DataSource = filtered;
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
