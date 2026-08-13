using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class QualityInspectionEditForm : Form
{
    private readonly QualityInspectionService _service;
    private readonly QualityInspection? _existing;

    private readonly ComboBox _serializedItemBox = new ComboBox().StyleAsInput();
    private readonly TextBox _inspectorBox = new TextBox().StyleAsInput();
    private readonly ComboBox _resultBox = new ComboBox().StyleAsInput();
    private readonly TextBox _notesBox = new TextBox().StyleAsInput();
    private readonly Label _errorLabel;

    public QualityInspectionEditForm(QualityInspectionService service, List<SerializedItem> serializedItems, QualityInspection? existing)
    {
        _service = service;
        _existing = existing;

        Text = existing is null ? "New Quality Inspection" : $"Inspection - {existing.SerializedItem?.SerialNumber}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 320);
        BackColor = Theme.CardBackground;

        _resultBox.DataSource = Enum.GetValues<QualityResult>();
        _resultBox.EnableEnumDisplayFormat();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        if (existing is null)
        {
            _serializedItemBox.DisplayMember = "SerialNumber";
            _serializedItemBox.ValueMember = "SerializedItemId";
            _serializedItemBox.DataSource = serializedItems;
            FormLayoutHelper.AddRow(layout, 0, "Serialized Item*", _serializedItemBox);
            _resultBox.SelectedItem = QualityResult.Pending;
        }
        else
        {
            FormLayoutHelper.AddRow(layout, 0, "Serialized Item", new Label { Text = existing.SerializedItem?.SerialNumber ?? "-", Font = Theme.FontBodyBold, TextAlign = ContentAlignment.MiddleLeft });

            // ComboBox.SelectedItem only takes effect once the control's native handle
            // exists, so this must wait for Load rather than run here.
            Load += (_, _) => _resultBox.SelectedItem = existing.Result;
        }

        FormLayoutHelper.AddRow(layout, 1, "Inspector*", _inspectorBox);
        FormLayoutHelper.AddRow(layout, 2, "Result", _resultBox);
        FormLayoutHelper.AddRow(layout, 3, "Notes", _notesBox);
        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 4);

        Controls.Add(layout);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 0, 24, 16) };
        var saveButton = new Button { Text = "Save", Width = 100, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        saveButton.Click += async (_, _) => await SaveAsync();
        var cancelButton = new Button { Text = "Cancel", Width = 100, Dock = DockStyle.Right, Margin = new Padding(0, 0, 8, 0) }.StyleAsSecondaryButton();
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bar.Controls.Add(saveButton);
        bar.Controls.Add(cancelButton);
        Controls.Add(bar);

        if (existing is not null)
        {
            _inspectorBox.Text = existing.Inspector;
            _notesBox.Text = existing.Notes;
        }
    }

    private async Task SaveAsync()
    {
        _errorLabel.Text = string.Empty;
        try
        {
            var result = (QualityResult)(_resultBox.SelectedItem ?? QualityResult.Pending);

            if (_existing is null)
            {
                var serializedItemId = (int)(_serializedItemBox.SelectedValue ?? 0);
                await _service.CreateAsync(serializedItemId, _inspectorBox.Text.Trim(), result, _notesBox.Text.Trim());
            }
            else
            {
                await _service.UpdateAsync(_existing, _inspectorBox.Text.Trim(), result, _notesBox.Text.Trim());
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
            _errorLabel.Text = "Unable to save the inspection. Please try again.";
        }
    }
}
