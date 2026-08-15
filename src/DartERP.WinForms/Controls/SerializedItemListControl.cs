using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class SerializedItemListControl : UserControl
{
    private readonly SerializedItemService _service;
    private readonly WorkOrderService _workOrderService;
    private readonly TextBox _searchBox;
    private readonly DataGridView _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;
    private List<SerializedItem> _allItems = [];

    public SerializedItemListControl(SerializedItemService service, WorkOrderService workOrderService)
    {
        _service = service;
        _workOrderService = workOrderService;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ Add Serialized Item", Width = 180, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync(null);

        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search by serial number..." }.StyleAsInput();
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "SerializedInventory.csv");

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenEditorAsync((SerializedItem)_grid.Rows[e.RowIndex].DataBoundItem);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "Serial Number", DataPropertyName = "SerialNumber", FillWeight = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", FillWeight = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkOrder", HeaderText = "Work Order", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Created", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 100 });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var item = (SerializedItem)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "Product":
                    e.Value = item.Product?.ProductName ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "WorkOrder":
                    e.Value = item.WorkOrder?.WorkOrderNumber ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "CreatedDate":
                    e.Value = item.CreatedDate.ToString("MM/dd/yyyy");
                    e.FormattingApplied = true;
                    break;
                case "Status" when e.CellStyle is not null:
                    e.Value = EnumDisplay.For(item.Status);
                    e.CellStyle.ForeColor = StatusColors.For(item.Status);
                    e.CellStyle.Font = Theme.FontBodyBold;
                    e.FormattingApplied = true;
                    break;
            }
        };
    }

    private async Task RefreshAsync()
    {
        _allItems = await _service.GetAllWithDetailsAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var term = _searchBox.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(term)
            ? _allItems
            : _allItems.Where(i => i.SerialNumber.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No serialized items found", string.Empty);
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
            _emptyState.Subtitle = string.IsNullOrWhiteSpace(term)
                ? "Serialized items are created against work orders for serialized products."
                : "Try a different search term.";
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = filtered;
        }
    }

    private async Task OpenEditorAsync(SerializedItem? existing)
    {
        if (existing is null)
        {
            var workOrders = await _workOrderService.GetAllWithProductAsync();
            var serializedWorkOrders = workOrders.Where(w => w.Product?.IsSerialized == true).ToList();

            if (serializedWorkOrders.Count == 0)
            {
                MessageBox.Show(FindForm(), "Create a work order for a serialized product before adding serialized items.",
                    "No eligible work orders", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var suggested = _service.SuggestNextSerialNumber(_allItems);
            using var form = new SerializedItemEditForm(_service, serializedWorkOrders, suggested, null);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await RefreshAsync();
        }
        else
        {
            using var form = new SerializedItemEditForm(_service, [], string.Empty, existing);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await RefreshAsync();
        }
    }
}
