using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class SerializedItemEditForm : Form
{
    private readonly SerializedItemService _service;
    private readonly SerializedItem? _existing;

    private readonly ComboBox _workOrderBox = new ComboBox().StyleAsInput();
    private readonly TextBox _serialNumberBox = new TextBox().StyleAsInput();
    private readonly ComboBox _statusBox = new ComboBox().StyleAsInput();
    private readonly Label _errorLabel;

    public SerializedItemEditForm(SerializedItemService service, List<WorkOrder> serializedWorkOrders, string suggestedSerialNumber, SerializedItem? existing)
    {
        _service = service;
        _existing = existing;

        Text = existing is null ? "New Serialized Item" : $"Serialized Item - {existing.SerialNumber}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 300);
        BackColor = Theme.CardBackground;

        _statusBox.DataSource = Enum.GetValues<SerializedItemStatus>();
        _statusBox.EnableEnumDisplayFormat();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        if (existing is null)
        {
            _workOrderBox.DisplayMember = "WorkOrderNumber";
            _workOrderBox.ValueMember = "WorkOrderId";
            _workOrderBox.DataSource = serializedWorkOrders;
            FormLayoutHelper.AddRow(layout, 0, "Work Order*", _workOrderBox);

            _serialNumberBox.Text = suggestedSerialNumber;
            FormLayoutHelper.AddRow(layout, 1, "Serial Number*", _serialNumberBox);

            _statusBox.SelectedItem = SerializedItemStatus.InProduction;
            FormLayoutHelper.AddRow(layout, 2, "Status", _statusBox);
        }
        else
        {
            FormLayoutHelper.AddRow(layout, 0, "Work Order", new Label { Text = existing.WorkOrder?.WorkOrderNumber ?? "-", Font = Theme.FontBodyBold, ForeColor = Theme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft });
            FormLayoutHelper.AddRow(layout, 1, "Serial Number", new Label { Text = existing.SerialNumber, Font = Theme.FontBodyBold, ForeColor = Theme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft });
            FormLayoutHelper.AddRow(layout, 2, "Status", _statusBox);

            // ComboBox.SelectedItem only takes effect once the control's native handle
            // exists, so this must wait for Load rather than run here.
            Load += (_, _) => _statusBox.SelectedItem = existing.Status;
        }

        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 3);

        Controls.Add(layout);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 0, 24, 16) };
        var saveButton = new Button { Text = "Save", Width = 100, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        saveButton.Click += async (_, _) => await SaveAsync();
        var cancelButton = new Button { Text = "Cancel", Width = 100, Dock = DockStyle.Right, Margin = new Padding(0, 0, 8, 0) }.StyleAsSecondaryButton();
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bar.Controls.Add(saveButton);
        bar.Controls.Add(cancelButton);
        Controls.Add(bar);
    }

    private async Task SaveAsync()
    {
        _errorLabel.Text = string.Empty;
        try
        {
            var status = (SerializedItemStatus)(_statusBox.SelectedItem ?? SerializedItemStatus.InProduction);

            if (_existing is null)
            {
                var workOrderId = (int)(_workOrderBox.SelectedValue ?? 0);
                await _service.CreateAsync(workOrderId, _serialNumberBox.Text.Trim(), status);
            }
            else
            {
                await _service.UpdateStatusAsync(_existing.SerializedItemId, status);
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
            _errorLabel.Text = "Unable to save the serialized item. Please try again.";
        }
    }
}
