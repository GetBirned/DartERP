using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class WorkOrderEditForm : Form
{
    private readonly WorkOrderService _service;
    private readonly List<Product> _activeProducts;
    private readonly WorkOrder? _existing;
    private readonly bool _isLocked;

    private readonly ComboBox _productBox = new ComboBox().StyleAsInput();
    private readonly NumericUpDown _quantityBox = new() { Minimum = 1, Maximum = 999999, Value = 1, Font = Theme.FontBody };
    private readonly DateTimePicker _startDatePicker = new() { Font = Theme.FontBody, Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _dueDatePicker = new() { Font = Theme.FontBody, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _statusBox = new ComboBox().StyleAsInput();
    private readonly TextBox _notesBox = new TextBox().StyleAsInput();
    private readonly Label _errorLabel;
    private readonly Button _saveButton;

    public WorkOrderEditForm(WorkOrderService service, List<Product> activeProducts, WorkOrder? existing)
    {
        _service = service;
        _activeProducts = activeProducts;
        _existing = existing;
        _isLocked = existing is not null && WorkOrderService.IsLocked(existing.Status);

        Text = existing is null ? "New Work Order" : $"Work Order - {existing.WorkOrderNumber}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 420);
        BackColor = Theme.CardBackground;

        _productBox.DisplayMember = "ProductName";
        _productBox.ValueMember = "ProductId";
        _productBox.DataSource = _activeProducts;
        _statusBox.DataSource = Enum.GetValues<WorkOrderStatus>();
        _statusBox.EnableEnumDisplayFormat();
        _startDatePicker.Value = DateTime.Today;
        _dueDatePicker.Value = DateTime.Today.AddDays(14);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        FormLayoutHelper.AddRow(layout, 0, "Product*", _productBox);
        FormLayoutHelper.AddRow(layout, 1, "Quantity*", _quantityBox);
        FormLayoutHelper.AddRow(layout, 2, "Start Date", _startDatePicker);
        FormLayoutHelper.AddRow(layout, 3, "Due Date", _dueDatePicker);
        FormLayoutHelper.AddRow(layout, 4, "Status", _statusBox);
        FormLayoutHelper.AddRow(layout, 5, "Notes", _notesBox);
        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 6);

        Controls.Add(layout);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 0, 24, 16) };
        _saveButton = new Button { Text = "Save", Width = 100, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        _saveButton.Click += async (_, _) => await SaveAsync();
        var cancelButton = new Button { Text = _isLocked ? "Close" : "Cancel", Width = 100, Dock = DockStyle.Right, Margin = new Padding(0, 0, 8, 0) }.StyleAsSecondaryButton();
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bar.Controls.Add(_saveButton);
        bar.Controls.Add(cancelButton);
        Controls.Add(bar);

        if (existing is not null)
        {
            // ComboBox.SelectedValue (DataSource+ValueMember binding) only takes effect
            // once the control's native handle exists, so this must wait for Load rather
            // than run here in the constructor.
            Load += (_, _) =>
            {
                LoadExisting(existing);
                if (_isLocked)
                    ApplyLockedState();
            };
        }
        else
        {
            _statusBox.SelectedItem = WorkOrderStatus.Planned;
        }
    }

    private void LoadExisting(WorkOrder w)
    {
        _productBox.SelectedValue = w.ProductId;
        _quantityBox.Value = w.Quantity;
        _startDatePicker.Value = w.StartDate.Date;
        _dueDatePicker.Value = w.DueDate.Date;
        _statusBox.SelectedItem = w.Status;
        _notesBox.Text = w.Notes;
    }

    private void ApplyLockedState()
    {
        _productBox.Enabled = false;
        _quantityBox.Enabled = false;
        _startDatePicker.Enabled = false;
        _dueDatePicker.Enabled = false;
        _statusBox.Enabled = false;
        _notesBox.Enabled = false;
        _saveButton.Visible = false;
        _errorLabel.Text = "This work order is completed or cancelled and can no longer be edited.";
        _errorLabel.ForeColor = Theme.TextSecondary;
    }

    private async Task SaveAsync()
    {
        _errorLabel.Text = string.Empty;
        try
        {
            var productId = (int)(_productBox.SelectedValue ?? 0);
            var status = (WorkOrderStatus)(_statusBox.SelectedItem ?? WorkOrderStatus.Planned);

            if (_existing is null)
            {
                await _service.CreateAsync(productId, (int)_quantityBox.Value, _startDatePicker.Value, _dueDatePicker.Value, _notesBox.Text.Trim(), status);
            }
            else
            {
                await _service.UpdateAsync(_existing, productId, (int)_quantityBox.Value, _startDatePicker.Value, _dueDatePicker.Value, _notesBox.Text.Trim(), status);
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
            _errorLabel.Text = "Unable to save the work order. Please try again.";
        }
    }
}
