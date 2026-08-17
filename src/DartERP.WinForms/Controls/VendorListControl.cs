using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Controls;

public class VendorListControl : UserControl
{
    private record VendorRow(Vendor Source, string VendorNumber, string CompanyName, string ContactName, string Email, string Phone, string VendorType, string Status);

    private readonly VendorService _service;
    private readonly TextBox _searchBox;
    private readonly CheckBox _activeOnlyCheck;
    private readonly SfDataGrid _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public VendorListControl(VendorService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Vendor", Width = 140, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync(null);

        _activeOnlyCheck = new CheckBox
        {
            Text = "Active only",
            Checked = true,
            AutoSize = true,
            Font = Theme.FontBody,
            Dock = DockStyle.Right,
            Width = 100,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _activeOnlyCheck.CheckedChanged += async (_, _) => await RefreshAsync();

        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search by company, contact, or vendor #..." }.StyleAsInput();
        _searchBox.TextChanged += async (_, _) => await RefreshAsync();

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "Vendors.csv");

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_activeOnlyCheck);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.DataRow.RowData is VendorRow row)
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
        _grid.Columns.Add(new GridTextColumn { MappingName = "VendorNumber", HeaderText = "Vendor #", Width = 90 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "CompanyName", HeaderText = "Company", Width = 170 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "ContactName", HeaderText = "Contact", Width = 130 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Email", HeaderText = "Email", Width = 170 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Phone", HeaderText = "Phone", Width = 110 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "VendorType", HeaderText = "Type", Width = 110 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Status", HeaderText = "Status", Width = 80 });

        _grid.QueryCellStyle += (_, e) =>
        {
            if (e.Column.MappingName != "Status" || e.DataRow.RowData is not VendorRow row)
                return;

            e.Style.TextColor = row.Status == "Active" ? Theme.SuccessGreen : Theme.NeutralGray;
            e.Style.Font.Bold = true;
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.SearchAsync(_searchBox.Text, _activeOnlyCheck.Checked);

        // Navigating away disposes this control while the search above is
        // still in flight — SfDataGrid throws on a DataSource assignment
        // after disposal (DataGridView never did), so this guard is load-bearing.
        if (IsDisposed)
            return;

        if (results.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No vendors found", string.Empty);
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
            _emptyState.Subtitle = string.IsNullOrWhiteSpace(_searchBox.Text)
                ? "Add your first vendor to get started."
                : "Try a different search term or clear the filter.";
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = results.Select(v => new VendorRow(
                v, v.VendorNumber, v.CompanyName, v.ContactName, v.Email, v.Phone,
                EnumDisplay.For(v.VendorType), v.IsActive ? "Active" : "Inactive")).ToList();
        }
    }

    private async Task OpenEditorAsync(Vendor? existing)
    {
        using var form = new VendorEditForm(_service, existing);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
