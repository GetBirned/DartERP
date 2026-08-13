using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class VendorEditForm : Form
{
    private readonly VendorService _service;
    private readonly Vendor? _existing;

    private readonly TextBox _companyNameBox = new TextBox().StyleAsInput();
    private readonly TextBox _contactNameBox = new TextBox().StyleAsInput();
    private readonly TextBox _emailBox = new TextBox().StyleAsInput();
    private readonly TextBox _phoneBox = new TextBox().StyleAsInput();
    private readonly ComboBox _vendorTypeBox = new ComboBox().StyleAsInput();
    private readonly Label _errorLabel;

    public VendorEditForm(VendorService service, Vendor? existing)
    {
        _service = service;
        _existing = existing;

        Text = existing is null ? "New Vendor" : $"Edit Vendor - {existing.VendorNumber}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 380);
        BackColor = Theme.CardBackground;

        _vendorTypeBox.DataSource = Enum.GetValues<VendorType>();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        FormLayoutHelper.AddRow(layout, 0, "Company Name*", _companyNameBox);
        FormLayoutHelper.AddRow(layout, 1, "Contact Name", _contactNameBox);
        FormLayoutHelper.AddRow(layout, 2, "Email", _emailBox);
        FormLayoutHelper.AddRow(layout, 3, "Phone", _phoneBox);
        FormLayoutHelper.AddRow(layout, 4, "Vendor Type", _vendorTypeBox);
        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 5);

        Controls.Add(layout);
        Controls.Add(BuildButtonBar());

        if (existing is not null)
            LoadExisting(existing);
        else
            _vendorTypeBox.SelectedItem = VendorType.RawMaterials;
    }

    private void LoadExisting(Vendor v)
    {
        _companyNameBox.Text = v.CompanyName;
        _contactNameBox.Text = v.ContactName;
        _emailBox.Text = v.Email;
        _phoneBox.Text = v.Phone;
        _vendorTypeBox.SelectedItem = v.VendorType;
    }

    private Panel BuildButtonBar()
    {
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 0, 24, 16) };

        var saveButton = new Button { Text = "Save", Width = 100, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        saveButton.Click += async (_, _) => await SaveAsync();

        var cancelButton = new Button { Text = "Cancel", Width = 100, Dock = DockStyle.Right, Margin = new Padding(0, 0, 8, 0) }.StyleAsSecondaryButton();
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        bar.Controls.Add(saveButton);
        bar.Controls.Add(cancelButton);

        if (_existing is not null)
        {
            var toggleButton = _existing.IsActive
                ? new Button { Text = "Deactivate", Width = 110, Dock = DockStyle.Left }.StyleAsDangerButton()
                : new Button { Text = "Reactivate", Width = 110, Dock = DockStyle.Left }.StyleAsSecondaryButton();
            toggleButton.Click += async (_, _) => await ToggleActiveAsync();
            bar.Controls.Add(toggleButton);
        }

        return bar;
    }

    private async Task SaveAsync()
    {
        _errorLabel.Text = string.Empty;
        try
        {
            var vendorType = (VendorType)(_vendorTypeBox.SelectedItem ?? VendorType.Other);

            if (_existing is null)
            {
                var vendor = new Vendor
                {
                    CompanyName = _companyNameBox.Text.Trim(),
                    ContactName = _contactNameBox.Text.Trim(),
                    Email = _emailBox.Text.Trim(),
                    Phone = _phoneBox.Text.Trim(),
                    VendorType = vendorType,
                };
                await _service.CreateAsync(vendor);
            }
            else
            {
                _existing.CompanyName = _companyNameBox.Text.Trim();
                _existing.ContactName = _contactNameBox.Text.Trim();
                _existing.Email = _emailBox.Text.Trim();
                _existing.Phone = _phoneBox.Text.Trim();
                _existing.VendorType = vendorType;
                await _service.UpdateAsync(_existing);
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
            _errorLabel.Text = "Unable to save the vendor. Please try again.";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (_existing is null)
            return;

        var goingActive = !_existing.IsActive;
        var verb = goingActive ? "reactivate" : "deactivate";
        var confirm = MessageBox.Show(this, $"Are you sure you want to {verb} {_existing.CompanyName}?",
            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            await _service.SetActiveStatusAsync(_existing.VendorId, goingActive);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Unable to update the vendor's status. Please try again.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
