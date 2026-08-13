using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class QualityControlListControl : UserControl
{
    private readonly QualityInspectionService _service;
    private readonly SerializedItemService _serializedItemService;
    private readonly DataGridView _grid;
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

        toolbar.Controls.Add(titleLabel);
        toolbar.Controls.Add(newButton);

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenEditorAsync((QualityInspection)_grid.Rows[e.RowIndex].DataBoundItem);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "Serial Number", FillWeight = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", FillWeight = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InspectionDate", HeaderText = "Inspection Date", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Inspector", HeaderText = "Inspector", DataPropertyName = "Inspector", FillWeight = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "Result", FillWeight = 90 });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var inspection = (QualityInspection)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "SerialNumber":
                    e.Value = inspection.SerializedItem?.SerialNumber ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "Product":
                    e.Value = inspection.SerializedItem?.Product?.ProductName ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "InspectionDate":
                    e.Value = inspection.InspectionDate.ToString("MM/dd/yyyy");
                    e.FormattingApplied = true;
                    break;
                case "Result" when e.CellStyle is not null:
                    e.Value = EnumDisplay.For(inspection.Result);
                    e.CellStyle.ForeColor = StatusColors.For(inspection.Result);
                    e.CellStyle.Font = Theme.FontBodyBold;
                    e.FormattingApplied = true;
                    break;
            }
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.GetAllWithDetailsAsync();

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
            _grid.DataSource = results;
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
