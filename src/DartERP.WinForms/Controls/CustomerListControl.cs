using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class CustomerListControl : UserControl
{
    private readonly CustomerService _service;
    private readonly TextBox _searchBox;
    private readonly CheckBox _activeOnlyCheck;
    private readonly DataGridView _grid;
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

        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search by company, contact, or customer #..." }.StyleAsInput();
        _searchBox.TextChanged += async (_, _) => await RefreshAsync();

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "Customers.csv");

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_activeOnlyCheck);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenEditorAsync((Customer)_grid.Rows[e.RowIndex].DataBoundItem);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerNumber", HeaderText = "Customer #", DataPropertyName = "CustomerNumber", FillWeight = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", HeaderText = "Company", DataPropertyName = "CompanyName", FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContactName", HeaderText = "Contact", DataPropertyName = "ContactName", FillWeight = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", DataPropertyName = "Email", FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Phone", DataPropertyName = "Phone", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "City", HeaderText = "City", DataPropertyName = "City", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "State", HeaderText = "State", DataPropertyName = "State", FillWeight = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 80 });

        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name != "Status" || e.RowIndex < 0 || e.CellStyle is null)
                return;

            var customer = (Customer)_grid.Rows[e.RowIndex].DataBoundItem;
            e.Value = customer.IsActive ? "Active" : "Inactive";
            e.CellStyle.ForeColor = customer.IsActive ? Theme.SuccessGreen : Theme.NeutralGray;
            e.CellStyle.Font = Theme.FontBodyBold;
            e.FormattingApplied = true;
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.SearchAsync(_searchBox.Text, _activeOnlyCheck.Checked);

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
            _grid.DataSource = results;
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
