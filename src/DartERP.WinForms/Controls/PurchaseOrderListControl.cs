using DartERP.Application.Services;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class PurchaseOrderListControl : UserControl
{
    private readonly PurchaseOrderService _service;
    private readonly VendorService _vendorService;
    private readonly ProductService _productService;
    private readonly DataGridView _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public PurchaseOrderListControl(PurchaseOrderService service, VendorService vendorService, ProductService productService)
    {
        _service = service;
        _vendorService = vendorService;
        _productService = productService;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ New Purchase Order", Width = 180, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync(null);

        var titleLabel = new Label
        {
            Text = "All purchase orders, most recent first",
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
                await OpenEditorAsync((PurchaseOrder)_grid.Rows[e.RowIndex].DataBoundItem);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseOrderNumber", HeaderText = "PO #", DataPropertyName = "PurchaseOrderNumber", FillWeight = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vendor", HeaderText = "Vendor", FillWeight = 190 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderDate", HeaderText = "Order Date", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpectedDate", HeaderText = "Expected Date", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "Total", FillWeight = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var po = (PurchaseOrder)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "Vendor":
                    e.Value = po.Vendor?.CompanyName ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "OrderDate":
                    e.Value = po.OrderDate.ToString("MM/dd/yyyy");
                    e.FormattingApplied = true;
                    break;
                case "ExpectedDate":
                    e.Value = po.ExpectedDate?.ToString("MM/dd/yyyy") ?? "-";
                    e.FormattingApplied = true;
                    break;
                case "TotalAmount":
                    e.Value = po.TotalAmount.ToString("C2");
                    e.FormattingApplied = true;
                    break;
                case "Status" when e.CellStyle is not null:
                    e.Value = EnumDisplay.For(po.Status);
                    e.CellStyle.ForeColor = StatusColors.For(po.Status);
                    e.CellStyle.Font = Theme.FontBodyBold;
                    e.FormattingApplied = true;
                    break;
            }
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.GetAllWithVendorAsync();

        if (results.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No purchase orders yet", "Create your first purchase order to get started.");
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

    private async Task OpenEditorAsync(PurchaseOrder? summary)
    {
        var activeVendors = await _vendorService.GetActiveAsync();
        var activeProducts = await _productService.GetActiveAsync();

        if (activeVendors.Count == 0)
        {
            MessageBox.Show(FindForm(), "Add at least one active vendor before creating a purchase order.",
                "No vendors available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PurchaseOrder? existing = summary is null ? null : await _service.GetWithLinesAsync(summary.PurchaseOrderId);

        using var form = new PurchaseOrderEditForm(_service, activeVendors, activeProducts, existing);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
