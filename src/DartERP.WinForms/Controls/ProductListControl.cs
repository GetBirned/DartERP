using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class ProductListControl : UserControl
{
    private readonly ProductService _service;
    private readonly TextBox _searchBox;
    private readonly CheckBox _activeOnlyCheck;
    private readonly DataGridView _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public ProductListControl(ProductService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Product", Width = 140, Dock = DockStyle.Right }.StyleAsPrimaryButton();
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

        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search by SKU or product name..." }.StyleAsInput();
        _searchBox.TextChanged += async (_, _) => await RefreshAsync();

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_activeOnlyCheck);
        toolbar.Controls.Add(newButton);

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsDataGrid();
        BuildColumns();
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenEditorAsync((Product)_grid.Rows[e.RowIndex].DataBoundItem);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SKU", HeaderText = "SKU", DataPropertyName = "SKU", FillWeight = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Product", DataPropertyName = "ProductName", FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "Unit Cost", DataPropertyName = "UnitCost", FillWeight = 90, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "Sale Price", DataPropertyName = "SalePrice", FillWeight = 90, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuantityOnHand", HeaderText = "Qty", DataPropertyName = "QuantityOnHand", FillWeight = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Serialized", HeaderText = "Serialized", FillWeight = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 80 });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var product = (Product)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "Category":
                    e.Value = EnumDisplay.For(product.Category);
                    e.FormattingApplied = true;
                    break;
                case "Serialized":
                    e.Value = product.IsSerialized ? "Yes" : "No";
                    e.FormattingApplied = true;
                    break;
                case "Status" when e.CellStyle is not null:
                    e.Value = product.IsActive ? "Active" : "Inactive";
                    e.CellStyle.ForeColor = product.IsActive ? Theme.SuccessGreen : Theme.NeutralGray;
                    e.CellStyle.Font = Theme.FontBodyBold;
                    e.FormattingApplied = true;
                    break;
            }
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.SearchAsync(_searchBox.Text, _activeOnlyCheck.Checked);

        if (results.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No products found", string.Empty);
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
            _emptyState.Subtitle = string.IsNullOrWhiteSpace(_searchBox.Text)
                ? "Add your first product to get started."
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

    private async Task OpenEditorAsync(Product? existing)
    {
        using var form = new ProductEditForm(_service, existing);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
