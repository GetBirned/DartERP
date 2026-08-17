using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;

namespace DartERP.WinForms.Controls;

public class CustomerListControl : UserControl
{
    // SfDataGrid's QueryCellStyle can only change how a cell looks, not what
    // value it shows (unlike DataGridView's CellFormatting, which could do
    // both) — so anything computed (Status, from IsActive) gets pre-projected
    // into a plain row type before binding, and QueryCellStyle just colors it.
    // Source keeps the real entity around so double-click can open the editor
    // without a second lookup.
    private record CustomerRow(Customer Source, string CustomerNumber, string CompanyName, string ContactName, string Email, string Phone, string City, string State, string Status);

    private readonly CustomerService _service;
    private readonly TextBox _searchBox;
    private readonly CheckBox _activeOnlyCheck;
    private readonly SfDataGrid _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public CustomerListControl(CustomerService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Customer", Width = 150, Dock = DockStyle.Right }.StyleAsPrimaryButton();
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

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "Customers.csv");

        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search by company, contact, or customer #..." }.StyleAsInput();
        _searchBox.TextChanged += async (_, _) => await RefreshAsync();

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_activeOnlyCheck);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.DataRow.RowData is CustomerRow row)
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
        _grid.Columns.Add(new GridTextColumn { MappingName = "CustomerNumber", HeaderText = "Customer #", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "CompanyName", HeaderText = "Company", Width = 190 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "ContactName", HeaderText = "Contact", Width = 140 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Email", HeaderText = "Email", Width = 190 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Phone", HeaderText = "Phone", Width = 120 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "City", HeaderText = "City", Width = 110 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "State", HeaderText = "State", Width = 70 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Status", HeaderText = "Status", Width = 90 });

        _grid.QueryCellStyle += (_, e) =>
        {
            if (e.Column.MappingName != "Status" || e.DataRow.RowData is not CustomerRow row)
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
            _emptyState ??= BuildEmptyState();
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
            _emptyState.Subtitle = string.IsNullOrWhiteSpace(_searchBox.Text)
                ? "Add your first customer to get started."
                : "Try a different search term or clear the filter.";
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = results.Select(c => new CustomerRow(
                c, c.CustomerNumber, c.CompanyName, c.ContactName, c.Email, c.Phone, c.City, c.State,
                c.IsActive ? "Active" : "Inactive")).ToList();
        }
    }

    private EmptyStateControl BuildEmptyState() => new("No customers found", string.Empty);

    private async Task OpenEditorAsync(Customer? existing)
    {
        using var form = new CustomerEditForm(_service, existing);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
