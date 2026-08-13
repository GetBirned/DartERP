using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class CustomerEditForm : Form
{
    private readonly CustomerService _service;
    private readonly Customer? _existing;

    private readonly TextBox _companyNameBox = new TextBox().StyleAsInput();
    private readonly TextBox _contactNameBox = new TextBox().StyleAsInput();
    private readonly TextBox _emailBox = new TextBox().StyleAsInput();
    private readonly TextBox _phoneBox = new TextBox().StyleAsInput();
    private readonly TextBox _addressBox = new TextBox().StyleAsInput();
    private readonly TextBox _cityBox = new TextBox().StyleAsInput();
    private readonly TextBox _stateBox = new TextBox().StyleAsInput();
    private readonly Label _errorLabel;

    public CustomerEditForm(CustomerService service, Customer? existing)
    {
        _service = service;
        _existing = existing;

        Text = existing is null ? "New Customer" : $"Edit Customer - {existing.CustomerNumber}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 430);
        BackColor = Theme.CardBackground;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        FormLayoutHelper.AddRow(layout, 0, "Company Name*", _companyNameBox);
        FormLayoutHelper.AddRow(layout, 1, "Contact Name", _contactNameBox);
        FormLayoutHelper.AddRow(layout, 2, "Email", _emailBox);
        FormLayoutHelper.AddRow(layout, 3, "Phone", _phoneBox);
        FormLayoutHelper.AddRow(layout, 4, "Address", _addressBox);
        FormLayoutHelper.AddRow(layout, 5, "City", _cityBox);
        FormLayoutHelper.AddRow(layout, 6, "State", _stateBox);
        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 7);

        Controls.Add(layout);
        Controls.Add(BuildButtonBar());

        if (existing is not null)
            LoadExisting(existing);
    }

    private void LoadExisting(Customer c)
    {
        _companyNameBox.Text = c.CompanyName;
        _contactNameBox.Text = c.ContactName;
        _emailBox.Text = c.Email;
        _phoneBox.Text = c.Phone;
        _addressBox.Text = c.Address;
        _cityBox.Text = c.City;
        _stateBox.Text = c.State;
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
            if (_existing is null)
            {
                var customer = new Customer
                {
                    CompanyName = _companyNameBox.Text.Trim(),
                    ContactName = _contactNameBox.Text.Trim(),
                    Email = _emailBox.Text.Trim(),
                    Phone = _phoneBox.Text.Trim(),
                    Address = _addressBox.Text.Trim(),
                    City = _cityBox.Text.Trim(),
                    State = _stateBox.Text.Trim(),
                };
                await _service.CreateAsync(customer);
            }
            else
            {
                _existing.CompanyName = _companyNameBox.Text.Trim();
                _existing.ContactName = _contactNameBox.Text.Trim();
                _existing.Email = _emailBox.Text.Trim();
                _existing.Phone = _phoneBox.Text.Trim();
                _existing.Address = _addressBox.Text.Trim();
                _existing.City = _cityBox.Text.Trim();
                _existing.State = _stateBox.Text.Trim();
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
            _errorLabel.Text = "Unable to save the customer. Please try again.";
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
            await _service.SetActiveStatusAsync(_existing.CustomerId, goingActive);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Unable to update the customer's status. Please try again.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
