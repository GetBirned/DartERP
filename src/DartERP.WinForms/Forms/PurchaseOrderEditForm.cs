using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.DTOs;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class PurchaseOrderEditForm : Form
{
    private const int ProductColumnIndex = 0;
    private const int QuantityColumnIndex = 1;
    private const int UnitCostColumnIndex = 2;
    private const int LineTotalColumnIndex = 3;
    private const int RemoveColumnIndex = 4;

    private readonly PurchaseOrderService _service;
    private readonly List<Vendor> _activeVendors;
    private readonly List<Product> _activeProducts;
    private readonly PurchaseOrder? _existing;

    private readonly ComboBox _vendorBox = new ComboBox().StyleAsInput();
    private readonly DateTimePicker _expectedDatePicker = new() { Font = Theme.FontBody, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _statusBox = new ComboBox().StyleAsInput();
    private readonly TextBox _notesBox = new TextBox().StyleAsInput();
    private readonly DataGridView _linesGrid;
    private readonly LetterSpacedLabel _totalLabel;
    private readonly Label _errorLabel;
    private readonly DashboardListCard _historyCard;

    public PurchaseOrderEditForm(PurchaseOrderService service, List<Vendor> activeVendors, List<Product> activeProducts, PurchaseOrder? existing)
    {
        _service = service;
        _activeVendors = activeVendors;
        _activeProducts = activeProducts;
        _existing = existing;

        Text = existing is null ? "New Purchase Order" : $"Edit Purchase Order - {existing.PurchaseOrderNumber}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(760, 560);
        BackColor = Theme.CardBackground;

        _statusBox.DataSource = Enum.GetValues<PurchaseOrderStatus>();
        _statusBox.EnableEnumDisplayFormat();
        _vendorBox.DisplayMember = "CompanyName";
        _vendorBox.ValueMember = "VendorId";
        _vendorBox.DataSource = _activeVendors;
        _expectedDatePicker.Value = DateTime.Today.AddDays(14);

        var headerPanel = BuildHeaderPanel();
        var linesLabel = new LetterSpacedLabel
        {
            Text = "Line Items",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(24, 8, 0, 0),
        };

        var addLineButton = new Button { Text = "+ Add Line", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        addLineButton.Click += (_, _) => AddLine(_activeProducts.FirstOrDefault()?.ProductId ?? 0, 1, _activeProducts.FirstOrDefault()?.UnitCost ?? 0);
        var addLineBar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(24, 4, 24, 4) };
        addLineBar.Controls.Add(addLineButton);

        _linesGrid = BuildLinesGrid();
        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 8) };
        gridHost.Controls.Add(_linesGrid);

        var footer = BuildFooterPanel(out _totalLabel, out _errorLabel);

        _historyCard = new DashboardListCard("Status History", "No status changes recorded yet.", stacked: true);
        var historyHost = new Panel { Dock = DockStyle.Right, Width = 276, Padding = new Padding(8, 0, 24, 8) };
        historyHost.Controls.Add(_historyCard);

        Controls.Add(gridHost);
        Controls.Add(historyHost);
        Controls.Add(addLineBar);
        Controls.Add(linesLabel);
        Controls.Add(headerPanel);
        Controls.Add(footer);

        if (existing is not null)
        {
            // ComboBox.SelectedValue (DataSource+ValueMember binding) only takes effect
            // once the control's native handle exists, so this must wait for Load rather
            // than run here in the constructor.
            Load += async (_, _) =>
            {
                LoadExisting(existing);
                await LoadHistoryAsync(existing.PurchaseOrderId);
            };
        }
        else
        {
            _statusBox.SelectedItem = PurchaseOrderStatus.Draft;
            _historyCard.SetRows(Array.Empty<DashboardListRow>());
        }

        RecalculateTotal();
    }

    private Panel BuildHeaderPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(24, 16, 24, 8),
            AutoSize = true,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var poNumberCaption = new Label { Text = "PO Number", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var poNumberValue = new Label
        {
            Name = "PoNumberValue",
            Text = _existing?.PurchaseOrderNumber ?? "(auto-generated)",
            Font = Theme.FontBodyBold,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(poNumberCaption, 0, 0);
        panel.Controls.Add(poNumberValue, 1, 0);

        var statusCaption = new Label { Text = "Status", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(statusCaption, 2, 0);
        _statusBox.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(_statusBox, 3, 0);

        var vendorCaption = new Label { Text = "Vendor*", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(vendorCaption, 0, 1);
        _vendorBox.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(_vendorBox, 1, 1);

        var expectedCaption = new Label { Text = "Expected Date", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(expectedCaption, 2, 1);
        _expectedDatePicker.Dock = DockStyle.Fill;
        _expectedDatePicker.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(_expectedDatePicker, 3, 1);

        var notesCaption = new Label { Text = "Notes", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(notesCaption, 0, 2);
        _notesBox.Dock = DockStyle.Fill;
        _notesBox.Margin = new Padding(0, 4, 0, 4);
        panel.SetColumnSpan(_notesBox, 3);
        panel.Controls.Add(_notesBox, 1, 2);

        return panel;
    }

    private DataGridView BuildLinesGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
        };
        grid.StyleAsDataGrid();
        grid.ReadOnly = false;
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        var productColumn = new DataGridViewComboBoxColumn
        {
            Name = "Product",
            HeaderText = "Product",
            DataSource = _activeProducts,
            DisplayMember = "ProductName",
            ValueMember = "ProductId",
            FillWeight = 220,
            FlatStyle = FlatStyle.Flat,
        };
        grid.Columns.Add(productColumn);

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity",
            HeaderText = "Quantity",
            FillWeight = 90,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight },
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "UnitCost",
            HeaderText = "Unit Cost",
            FillWeight = 100,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight },
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LineTotal",
            HeaderText = "Line Total",
            FillWeight = 100,
            ReadOnly = true,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, BackColor = Theme.AppBackground },
        });

        grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Remove",
            Text = "Remove",
            UseColumnTextForButtonValue = true,
            FillWeight = 70,
        });

        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == ProductColumnIndex)
                ApplyDefaultUnitCost(e.RowIndex);

            if (e.ColumnIndex is ProductColumnIndex or QuantityColumnIndex or UnitCostColumnIndex)
                RecalculateLine(e.RowIndex);
        };

        grid.CellEndEdit += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex is QuantityColumnIndex or UnitCostColumnIndex)
                RecalculateLine(e.RowIndex);
        };

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == RemoveColumnIndex)
            {
                grid.Rows.RemoveAt(e.RowIndex);
                RecalculateTotal();
            }
        };

        return grid;
    }

    private Panel BuildFooterPanel(out LetterSpacedLabel totalLabel, out Label errorLabel)
    {
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 76, Padding = new Padding(24, 8, 24, 16) };

        var saveButton = new Button { Text = "Save", Width = 110, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        saveButton.Click += async (_, _) => await SaveAsync();

        var cancelButton = new Button { Text = "Cancel", Width = 100, Dock = DockStyle.Right, Margin = new Padding(0, 0, 8, 0) }.StyleAsSecondaryButton();
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        totalLabel = new LetterSpacedLabel
        {
            Text = "Total: $0.00",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        errorLabel = new Label
        {
            Text = string.Empty,
            Font = Theme.FontSmall,
            ForeColor = Theme.DangerRed,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        footer.Controls.Add(saveButton);
        footer.Controls.Add(cancelButton);
        footer.Controls.Add(totalLabel);
        footer.Controls.Add(errorLabel);

        return footer;
    }

    private void LoadExisting(PurchaseOrder po)
    {
        _vendorBox.SelectedValue = po.VendorId;
        _expectedDatePicker.Value = po.ExpectedDate ?? DateTime.Today.AddDays(14);
        _statusBox.SelectedItem = po.Status;
        _notesBox.Text = po.Notes;

        foreach (var line in po.Lines)
            AddLine(line.ProductId, line.Quantity, line.UnitCost);
    }

    private async Task LoadHistoryAsync(int purchaseOrderId)
    {
        var history = await _service.GetStatusHistoryAsync(purchaseOrderId);
        var rows = history.Select(h => new DashboardListRow(
                h.FromStatus is null
                    ? $"Created as {EnumDisplay.For(h.ToStatus)}"
                    : $"{EnumDisplay.For(h.FromStatus.Value)} → {EnumDisplay.For(h.ToStatus)}",
                $"{h.ChangedByUser?.DisplayName ?? "Unknown"} · {h.ChangedAt.ToLocalTime():MM/dd/yyyy h:mm tt}"))
            .ToList();
        _historyCard.SetRows(rows);
    }

    private void AddLine(int productId, int quantity, decimal unitCost)
    {
        var rowIndex = _linesGrid.Rows.Add();
        var row = _linesGrid.Rows[rowIndex];
        row.Cells[ProductColumnIndex].Value = productId > 0 ? productId : (_activeProducts.FirstOrDefault()?.ProductId ?? 0);
        row.Cells[QuantityColumnIndex].Value = quantity;
        row.Cells[UnitCostColumnIndex].Value = unitCost.ToString("N2");
        row.Cells[LineTotalColumnIndex].Value = (quantity * unitCost).ToString("C2");
        RecalculateTotal();
    }

    private void ApplyDefaultUnitCost(int rowIndex)
    {
        var row = _linesGrid.Rows[rowIndex];
        if (row.Cells[ProductColumnIndex].Value is not int productId)
            return;

        var product = _activeProducts.FirstOrDefault(p => p.ProductId == productId);
        if (product is not null)
            row.Cells[UnitCostColumnIndex].Value = product.UnitCost.ToString("N2");
    }

    private void RecalculateLine(int rowIndex)
    {
        var row = _linesGrid.Rows[rowIndex];
        var quantity = ParseInt(row.Cells[QuantityColumnIndex].Value);
        var unitCost = ParseDecimal(row.Cells[UnitCostColumnIndex].Value);
        row.Cells[LineTotalColumnIndex].Value = (quantity * unitCost).ToString("C2");
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in _linesGrid.Rows)
        {
            var quantity = ParseInt(row.Cells[QuantityColumnIndex].Value);
            var unitCost = ParseDecimal(row.Cells[UnitCostColumnIndex].Value);
            total += quantity * unitCost;
        }
        _totalLabel.Text = $"Total: {total:C2}";
    }

    private static int ParseInt(object? value) => int.TryParse(value?.ToString(), out var result) ? result : 0;

    private static decimal ParseDecimal(object? value) => decimal.TryParse(value?.ToString(), out var result) ? result : 0m;

    private List<PurchaseOrderLineInput> CollectLines() =>
        _linesGrid.Rows.Cast<DataGridViewRow>()
            .Where(r => r.Cells[ProductColumnIndex].Value is int)
            .Select(r => new PurchaseOrderLineInput
            {
                ProductId = (int)r.Cells[ProductColumnIndex].Value!,
                Quantity = ParseInt(r.Cells[QuantityColumnIndex].Value),
                UnitCost = ParseDecimal(r.Cells[UnitCostColumnIndex].Value),
            })
            .ToList();

    private async Task SaveAsync()
    {
        _errorLabel.Text = string.Empty;
        try
        {
            var vendorId = (int)(_vendorBox.SelectedValue ?? 0);
            var status = (PurchaseOrderStatus)(_statusBox.SelectedItem ?? PurchaseOrderStatus.Draft);
            var lines = CollectLines();

            if (_existing is null)
            {
                await _service.CreateAsync(vendorId, _expectedDatePicker.Value, _notesBox.Text.Trim(), status, lines);
            }
            else
            {
                await _service.UpdateAsync(_existing.PurchaseOrderId, vendorId, _expectedDatePicker.Value, _notesBox.Text.Trim(), status, lines);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ValidationException ex)
        {
            _errorLabel.Text = ex.Message;
        }
        catch (Exception)
        {
            _errorLabel.Text = "Unable to save the purchase order. Please verify the required information and try again.";
        }
    }
}
