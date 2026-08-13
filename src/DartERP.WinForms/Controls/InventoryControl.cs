using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class InventoryControl : UserControl
{
    private readonly InventoryService _service;
    private readonly FlowLayoutPanel _kpiPanel;
    private readonly DataGridView _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public InventoryControl(InventoryService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        _kpiPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 116,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        var sectionLabel = new Label
        {
            Text = "Inventory by Product",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsDataGrid();
        BuildColumns();

        _gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground };
        _gridHost.Controls.Add(_grid);

        Controls.Add(_gridHost);
        Controls.Add(sectionLabel);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16 });
        Controls.Add(_kpiPanel);

        Load += async (_, _) => await RefreshAsync();
    }

    private void BuildColumns()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SKU", HeaderText = "SKU", DataPropertyName = "SKU", FillWeight = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Product", DataPropertyName = "ProductName", FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuantityOnHand", HeaderText = "Qty On Hand", DataPropertyName = "QuantityOnHand", FillWeight = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReorderLevel", HeaderText = "Reorder Level", DataPropertyName = "ReorderLevel", FillWeight = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Inventory Value", FillWeight = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Serialized", HeaderText = "Serialized", FillWeight = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockStatus", HeaderText = "Stock Status", FillWeight = 100 });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var product = (Product)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;
            var lowStock = product.QuantityOnHand <= product.ReorderLevel;

            switch (columnName)
            {
                case "Category":
                    e.Value = EnumDisplay.For(product.Category);
                    e.FormattingApplied = true;
                    break;
                case "Value":
                    e.Value = (product.UnitCost * product.QuantityOnHand).ToString("C2");
                    e.FormattingApplied = true;
                    break;
                case "Serialized":
                    e.Value = product.IsSerialized ? "Yes" : "No";
                    e.FormattingApplied = true;
                    break;
                case "StockStatus" when e.CellStyle is not null:
                    e.Value = lowStock ? "Below Reorder" : "OK";
                    e.CellStyle.ForeColor = lowStock ? Theme.WarningAmber : Theme.SuccessGreen;
                    e.CellStyle.Font = Theme.FontBodyBold;
                    e.FormattingApplied = true;
                    break;
            }

            if (lowStock && e.CellStyle is not null)
                e.CellStyle.BackColor = Color.FromArgb(255, 251, 235);
        };
    }

    private async Task RefreshAsync()
    {
        var summary = await _service.GetSummaryAsync();

        _kpiPanel.Controls.Clear();
        _kpiPanel.Controls.Add(new KpiCard("Inventory Value", summary.TotalInventoryValue.ToString("C0")) { AccentColor = Theme.AccentBlue });
        _kpiPanel.Controls.Add(new KpiCard("Active Products", summary.ActiveProductCount.ToString()) { AccentColor = Theme.SuccessGreen });
        _kpiPanel.Controls.Add(new KpiCard("Serialized Products", summary.SerializedProductCount.ToString()) { AccentColor = Theme.AccentBlue });
        _kpiPanel.Controls.Add(new KpiCard("Below Reorder Level", summary.BelowReorderCount.ToString())
        {
            AccentColor = summary.BelowReorderCount > 0 ? Theme.WarningAmber : Theme.SuccessGreen,
        });

        var products = await _service.GetActiveProductsAsync();

        if (products.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No inventory to show", "Add products to start tracking inventory.");
            if (!_gridHost.Controls.Contains(_emptyState))
                _gridHost.Controls.Add(_emptyState);
        }
        else
        {
            if (_emptyState is not null)
                _gridHost.Controls.Remove(_emptyState);
            _grid.Visible = true;
            _grid.DataSource = products.OrderBy(p => p.QuantityOnHand - p.ReorderLevel).ToList();
        }
    }
}
