using System.ComponentModel;
using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.DTOs;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Forms;

public class PurchaseOrderEditForm : Form
{
    // SfDataGrid binds the line-items grid to this instead of raw
    // DataGridViewRow cell access. INotifyPropertyChanged is what makes the
    // Line Total column (and the footer total, via _lines.ListChanged)
    // update itself the moment Quantity or UnitCost changes in the grid —
    // BindingList<T> auto-subscribes to PropertyChanged on every item it
    // holds when T implements the interface.
    private sealed class PurchaseOrderLineRow : INotifyPropertyChanged
    {
        private int _productId;
        private int _quantity;
        private decimal _unitCost;

        public int ProductId
        {
            get => _productId;
            set { _productId = value; OnPropertyChanged(nameof(ProductId)); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(LineTotal)); }
        }

        public decimal UnitCost
        {
            get => _unitCost;
            set { _unitCost = value; OnPropertyChanged(nameof(UnitCost)); OnPropertyChanged(nameof(LineTotal)); }
        }

        public decimal LineTotal => Quantity * UnitCost;

        // GridButtonColumn.MappingName still has to resolve to a real
        // property — without one, the grid never establishes the row
        // binding for that column, and CellButtonClick silently never fires.
        public string Remove => string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly PurchaseOrderService _service;
    private readonly List<Vendor> _activeVendors;
    private readonly List<Product> _activeProducts;
    private readonly PurchaseOrder? _existing;
    private readonly BindingList<PurchaseOrderLineRow> _lines = [];

    private readonly ComboBox _vendorBox = new ComboBox().StyleAsInput();
    private readonly DateTimePicker _expectedDatePicker = new() { Font = Theme.FontBody, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _statusBox = new ComboBox().StyleAsInput();
    private readonly TextBox _notesBox = new TextBox().StyleAsInput();
    private readonly SfDataGrid _linesGrid;
    private readonly LetterSpacedLabel _totalLabel;
    private readonly Label _errorLabel;
    private readonly DashboardListCard _historyCard;
    private readonly PurchaseOrderAttachmentsPanel _attachmentsCard;

    private const long MaxAttachmentSizeBytes = 25 * 1024 * 1024;

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
        ClientSize = new Size(760, 680);
        MinimumSize = new Size(760, 620);
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
        _attachmentsCard = new PurchaseOrderAttachmentsPanel();
        _attachmentsCard.AddRequested += async () => await AddAttachmentAsync();
        _attachmentsCard.RemoveRequested += async attachment => await RemoveAttachmentAsync(attachment);

        var sidePanel = new Panel { Dock = DockStyle.Right, Width = 276, Padding = new Padding(8, 0, 24, 8) };
        sidePanel.Controls.Add(_historyCard);
        sidePanel.Controls.Add(_attachmentsCard);

        Controls.Add(gridHost);
        Controls.Add(sidePanel);
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
                await LoadAttachmentsAsync(existing.PurchaseOrderId);
            };
        }
        else
        {
            _statusBox.SelectedItem = PurchaseOrderStatus.Draft;
            _historyCard.SetRows(Array.Empty<DashboardListRow>());
            _attachmentsCard.SetAttachments(Array.Empty<PurchaseOrderAttachment>());
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

    private SfDataGrid BuildLinesGrid()
    {
        var grid = new SfDataGrid
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
        };
        grid.StyleAsSfDataGrid();
        grid.AllowEditing = true;

        grid.Columns.Add(new GridComboBoxColumn
        {
            MappingName = "ProductId",
            HeaderText = "Product",
            DataSource = _activeProducts,
            DisplayMember = "ProductName",
            ValueMember = "ProductId",
            Width = 220,
        });

        grid.Columns.Add(new GridNumericColumn
        {
            MappingName = "Quantity",
            HeaderText = "Quantity",
            Width = 90,
            NumberFormatInfo = new System.Globalization.NumberFormatInfo { NumberDecimalDigits = 0 },
        });

        grid.Columns.Add(new GridNumericColumn
        {
            MappingName = "UnitCost",
            HeaderText = "Unit Cost",
            Width = 100,
            NumberFormatInfo = new System.Globalization.NumberFormatInfo { NumberDecimalDigits = 2 },
        });

        grid.Columns.Add(new GridTextColumn
        {
            MappingName = "LineTotal",
            HeaderText = "Line Total",
            Width = 100,
            AllowEditing = false,
            Format = "C2",
        });

        grid.Columns.Add(new GridButtonColumn
        {
            MappingName = "Remove",
            HeaderText = "Remove",
            AllowDefaultButtonText = true,
            DefaultButtonText = "Remove",
            Width = 70,
        });

        // Picking a different product doesn't touch Quantity/UnitCost through
        // the grid's own binding (ProductId is the only two-way-bound
        // property on that column) — this is what carries over the old
        // ApplyDefaultUnitCost behavior of defaulting the cost to the
        // product's list price whenever the product changes.
        grid.CellComboBoxSelectionChanged += (_, e) =>
        {
            if (UnwrapRow(e.Record) is PurchaseOrderLineRow row && e.SelectedItem is Product product)
                row.UnitCost = product.UnitCost;
        };

        grid.CellButtonClick += (_, e) =>
        {
            if (UnwrapRow(e.Record) is PurchaseOrderLineRow row)
                _lines.Remove(row);
        };

        _lines.ListChanged += (_, _) => RecalculateTotal();
        grid.DataSource = _lines;

        return grid;
    }

    // CellButtonClickEventArgs.Record and CellComboBoxSelectionChangedEventArgs.Record
    // are, despite the name, the grid's internal DataRow wrapper rather than the bound
    // object itself — the actual row is one level down, on DataRowBase.RowData.
    private static object? UnwrapRow(object? record) =>
        record is Syncfusion.WinForms.DataGrid.DataRowBase dataRow ? dataRow.RowData : record;

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

    private async Task LoadAttachmentsAsync(int purchaseOrderId)
    {
        var attachments = await _service.GetAttachmentsAsync(purchaseOrderId);
        _attachmentsCard.SetAttachments(attachments);
    }

    private async Task AddAttachmentAsync()
    {
        if (_existing is null)
        {
            _errorLabel.Text = "Save the purchase order before attaching files.";
            return;
        }

        using var dialog = new OpenFileDialog { Filter = "All Files (*.*)|*.*", Title = "Attach File" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var fileInfo = new FileInfo(dialog.FileName);
        if (fileInfo.Length > MaxAttachmentSizeBytes)
        {
            _errorLabel.Text = "Attachments must be smaller than 25 MB.";
            return;
        }

        _errorLabel.Text = string.Empty;
        var storedPath = PurchaseOrderAttachmentStore.SaveFromFile(_existing.PurchaseOrderId, dialog.FileName);
        await _service.AddAttachmentAsync(_existing.PurchaseOrderId, fileInfo.Name, storedPath, fileInfo.Length);
        await LoadAttachmentsAsync(_existing.PurchaseOrderId);
    }

    private async Task RemoveAttachmentAsync(PurchaseOrderAttachment attachment)
    {
        var confirm = MessageBox.Show(this, $"Remove \"{attachment.FileName}\"?", "Remove Attachment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        await _service.RemoveAttachmentAsync(attachment.PurchaseOrderAttachmentId);
        PurchaseOrderAttachmentStore.Delete(attachment.StoredPath);
        await LoadAttachmentsAsync(attachment.PurchaseOrderId);
    }

    private void AddLine(int productId, int quantity, decimal unitCost)
    {
        _lines.Add(new PurchaseOrderLineRow
        {
            ProductId = productId > 0 ? productId : (_activeProducts.FirstOrDefault()?.ProductId ?? 0),
            Quantity = quantity,
            UnitCost = unitCost,
        });
    }

    private void RecalculateTotal()
    {
        var total = _lines.Sum(r => r.LineTotal);
        _totalLabel.Text = $"Total: {total:C2}";
    }

    private List<PurchaseOrderLineInput> CollectLines() =>
        _lines.Where(r => r.ProductId > 0)
            .Select(r => new PurchaseOrderLineInput
            {
                ProductId = r.ProductId,
                Quantity = r.Quantity,
                UnitCost = r.UnitCost,
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
