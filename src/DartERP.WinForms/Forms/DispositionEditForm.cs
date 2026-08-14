using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

/// <summary>
/// Records a new disposition. Create-only by design — a bound-book entry
/// is a permanent record, so there's no edit-existing flow here the way
/// every other module's edit form has one.
/// </summary>
public class DispositionEditForm : Form
{
    private readonly DispositionService _service;

    private readonly ComboBox _serializedItemBox = new ComboBox().StyleAsInput();
    private readonly DateTimePicker _dispositionDatePicker = new() { Font = Theme.FontBody, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _typeBox = new ComboBox().StyleAsInput();
    private readonly ComboBox _customerBox = new ComboBox().StyleAsInput();
    private readonly Label _customerCaption;
    private readonly TextBox _notesBox = new TextBox().StyleAsInput();
    private readonly Label _errorLabel;

    public DispositionEditForm(DispositionService service, List<SerializedItem> availableItems, List<Customer> activeCustomers)
    {
        _service = service;

        Text = "Record Disposition";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 340);
        BackColor = Theme.CardBackground;

        _serializedItemBox.DisplayMember = "SerialNumber";
        _serializedItemBox.ValueMember = "SerializedItemId";
        _serializedItemBox.DataSource = availableItems;

        _typeBox.DataSource = Enum.GetValues<DispositionType>();
        _typeBox.EnableEnumDisplayFormat();
        _typeBox.SelectedIndexChanged += (_, _) => UpdateCustomerFieldState();

        _customerBox.DisplayMember = "CompanyName";
        _customerBox.ValueMember = "CustomerId";
        _customerBox.DataSource = activeCustomers;

        _dispositionDatePicker.Value = DateTime.Today;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        FormLayoutHelper.AddRow(layout, 0, "Serialized Item*", _serializedItemBox);
        FormLayoutHelper.AddRow(layout, 1, "Disposition Date", _dispositionDatePicker);
        FormLayoutHelper.AddRow(layout, 2, "Type", _typeBox);
        _customerCaption = FormLayoutHelper.AddRow(layout, 3, "Recipient*", _customerBox);
        FormLayoutHelper.AddRow(layout, 4, "Notes", _notesBox);
        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 5);

        Controls.Add(layout);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 0, 24, 16) };
        var saveButton = new Button { Text = "Save", Width = 100, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        saveButton.Click += async (_, _) => await SaveAsync();
        var cancelButton = new Button { Text = "Cancel", Width = 100, Dock = DockStyle.Right, Margin = new Padding(0, 0, 8, 0) }.StyleAsSecondaryButton();
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bar.Controls.Add(saveButton);
        bar.Controls.Add(cancelButton);
        Controls.Add(bar);

        // DataSource assignment above already auto-selects the first bound
        // item (DispositionType.Sold), so no explicit SelectedItem needed.
        UpdateCustomerFieldState();
    }

    private void UpdateCustomerFieldState()
    {
        var type = (DispositionType)(_typeBox.SelectedItem ?? DispositionType.Sold);
        var required = DispositionService.RequiresCustomer(type);
        _customerBox.Enabled = required || type == DispositionType.Returned;
        _customerCaption.Text = required ? "Recipient*" : "Recipient";
    }

    private async Task SaveAsync()
    {
        _errorLabel.Text = string.Empty;
        try
        {
            var serializedItemId = (int)(_serializedItemBox.SelectedValue ?? 0);
            var type = (DispositionType)(_typeBox.SelectedItem ?? DispositionType.Sold);
            int? customerId = _customerBox.Enabled && _customerBox.SelectedValue is int id ? id : null;

            await _service.CreateAsync(serializedItemId, _dispositionDatePicker.Value, type, customerId, _notesBox.Text.Trim());

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ValidationException ex)
        {
            _errorLabel.Text = ex.Message;
        }
        catch (Exception)
        {
            _errorLabel.Text = "Unable to record the disposition. Please try again.";
        }
    }
}
