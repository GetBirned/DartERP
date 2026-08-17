using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Controls;

public class QualityControlListControl : UserControl
{
    private record QualityInspectionRow(QualityInspection Source, string SerialNumber, string Product, string InspectionDate, string Inspector, string Result, Color ResultColor);

    private readonly QualityInspectionService _service;
    private readonly SerializedItemService _serializedItemService;
    private readonly SfDataGrid _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public QualityControlListControl(QualityInspectionService service, SerializedItemService serializedItemService)
    {
        _service = service;
        _serializedItemService = serializedItemService;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Inspection", Width = 150, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync(null);

        var titleLabel = new Label
        {
            Text = "All quality inspections, most recent first",
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "QualityControl.csv");

        toolbar.Controls.Add(titleLabel);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.DataRow.RowData is QualityInspectionRow row)
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
        _grid.Columns.Add(new GridTextColumn { MappingName = "SerialNumber", HeaderText = "Serial Number", Width = 150 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Product", HeaderText = "Product", Width = 160 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "InspectionDate", HeaderText = "Inspection Date", Width = 110 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Inspector", HeaderText = "Inspector", Width = 120 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Result", HeaderText = "Result", Width = 90 });

        _grid.QueryCellStyle += (_, e) =>
        {
            if (e.Column.MappingName != "Result" || e.DataRow.RowData is not QualityInspectionRow row)
                return;

            e.Style.TextColor = row.ResultColor;
            e.Style.Font.Bold = true;
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.GetAllWithDetailsAsync();

        // Navigating away disposes this control while the query above is
        // still in flight — SfDataGrid throws on a DataSource assignment
        // after disposal (DataGridView never did), so this guard is load-bearing.
        if (IsDisposed)
            return;

        if (results.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No quality inspections yet", "Add an inspection for a serialized item to get started.");
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = results.Select(i => new QualityInspectionRow(
                i, i.SerializedItem?.SerialNumber ?? string.Empty, i.SerializedItem?.Product?.ProductName ?? string.Empty,
                i.InspectionDate.ToString("MM/dd/yyyy"), i.Inspector, EnumDisplay.For(i.Result), StatusColors.For(i.Result))).ToList();
        }
    }

    private async Task OpenEditorAsync(QualityInspection? existing)
    {
        if (existing is null)
        {
            var serializedItems = await _serializedItemService.GetAllWithDetailsAsync();

            if (serializedItems.Count == 0)
            {
                MessageBox.Show(FindForm(), "Add a serialized item before creating a quality inspection.",
                    "No serialized items available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var form = new QualityInspectionEditForm(_service, serializedItems, null);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await RefreshAsync();
        }
        else
        {
            using var form = new QualityInspectionEditForm(_service, [], existing);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await RefreshAsync();
        }
    }
}
