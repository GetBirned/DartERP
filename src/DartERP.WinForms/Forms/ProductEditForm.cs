using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class ProductEditForm : Form
{
    private readonly ProductService _service;
    private readonly Product? _existing;

    private readonly TextBox _skuBox = new TextBox().StyleAsInput();
    private readonly TextBox _nameBox = new TextBox().StyleAsInput();
    private readonly ComboBox _categoryBox = new ComboBox().StyleAsInput();
    private readonly ProductIcon _categoryIcon = new(ProductCategory.FinishedProduct);
    private readonly TextBox _descriptionBox = new TextBox().StyleAsInput();
    private readonly NumericUpDown _unitCostBox = new NumericUpDown { DecimalPlaces = 2, Maximum = 999999 }.StyleAsInput();
    private readonly NumericUpDown _salePriceBox = new NumericUpDown { DecimalPlaces = 2, Maximum = 999999 }.StyleAsInput();
    private readonly NumericUpDown _quantityOnHandBox = new NumericUpDown { Maximum = 999999 }.StyleAsInput();
    private readonly NumericUpDown _reorderLevelBox = new NumericUpDown { Maximum = 999999 }.StyleAsInput();
    private readonly CheckBox _isSerializedBox = new() { Text = "Serialized product", Font = Theme.FontBody, AutoSize = true };
    private readonly Label _errorLabel;

    public ProductEditForm(ProductService service, Product? existing)
    {
        _service = service;
        _existing = existing;

        Text = existing is null ? "New Product" : $"Edit Product - {existing.SKU}";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(480, 520);
        BackColor = Theme.CardBackground;

        _categoryBox.DataSource = Enum.GetValues<ProductCategory>();
        _categoryBox.EnableEnumDisplayFormat();
        _categoryBox.SelectedIndexChanged += (_, _) =>
        {
            if (_categoryBox.SelectedItem is ProductCategory category)
                _categoryIcon.Category = category;
        };

        // Plain Panel + Dock ignores Margin, so the gap before the icon
        // comes from Width padding baked into the icon control itself.
        var categoryRow = new Panel { Height = 28 };
        _categoryIcon.Width = 30;
        _categoryIcon.Dock = DockStyle.Right;
        _categoryBox.Dock = DockStyle.Fill;
        categoryRow.Controls.Add(_categoryBox);
        categoryRow.Controls.Add(_categoryIcon);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(24, 20, 24, 0),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        FormLayoutHelper.AddRow(layout, 0, "SKU*", _skuBox);
        FormLayoutHelper.AddRow(layout, 1, "Product Name*", _nameBox);
        FormLayoutHelper.AddRow(layout, 2, "Category", categoryRow);
        FormLayoutHelper.AddRow(layout, 3, "Description", _descriptionBox);
        FormLayoutHelper.AddRow(layout, 4, "Unit Cost*", _unitCostBox);
        FormLayoutHelper.AddRow(layout, 5, "Sale Price*", _salePriceBox);
        FormLayoutHelper.AddRow(layout, 6, "Qty On Hand*", _quantityOnHandBox);
        FormLayoutHelper.AddRow(layout, 7, "Reorder Level*", _reorderLevelBox);
        FormLayoutHelper.AddRow(layout, 8, string.Empty, _isSerializedBox);
        _errorLabel = FormLayoutHelper.AddValidationLabel(layout, 9);

        Controls.Add(layout);
        Controls.Add(BuildButtonBar());

        if (existing is not null)
        {
            // ComboBox.SelectedItem/SelectedValue only takes effect once the control's
            // native handle exists, so this must wait for Load rather than run here.
            Load += (_, _) => LoadExisting(existing);
        }
        else
        {
            _categoryBox.SelectedItem = ProductCategory.FinishedProduct;
        }
    }

    private void LoadExisting(Product p)
    {
        _skuBox.Text = p.SKU;
        _nameBox.Text = p.ProductName;
        _categoryBox.SelectedItem = p.Category;
        _descriptionBox.Text = p.Description;
        _unitCostBox.Value = p.UnitCost;
        _salePriceBox.Value = p.SalePrice;
        _quantityOnHandBox.Value = p.QuantityOnHand;
        _reorderLevelBox.Value = p.ReorderLevel;
        _isSerializedBox.Checked = p.IsSerialized;
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
            var category = (ProductCategory)(_categoryBox.SelectedItem ?? ProductCategory.Other);

            if (_existing is null)
            {
                var product = new Product
                {
                    SKU = _skuBox.Text.Trim(),
                    ProductName = _nameBox.Text.Trim(),
                    Category = category,
                    Description = _descriptionBox.Text.Trim(),
                    UnitCost = _unitCostBox.Value,
                    SalePrice = _salePriceBox.Value,
                    QuantityOnHand = (int)_quantityOnHandBox.Value,
                    ReorderLevel = (int)_reorderLevelBox.Value,
                    IsSerialized = _isSerializedBox.Checked,
                };
                await _service.CreateAsync(product);
            }
            else
            {
                _existing.SKU = _skuBox.Text.Trim();
                _existing.ProductName = _nameBox.Text.Trim();
                _existing.Category = category;
                _existing.Description = _descriptionBox.Text.Trim();
                _existing.UnitCost = _unitCostBox.Value;
                _existing.SalePrice = _salePriceBox.Value;
                _existing.QuantityOnHand = (int)_quantityOnHandBox.Value;
                _existing.ReorderLevel = (int)_reorderLevelBox.Value;
                _existing.IsSerialized = _isSerializedBox.Checked;
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
            _errorLabel.Text = "Unable to save the product. Please try again.";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (_existing is null)
            return;

        var goingActive = !_existing.IsActive;
        var verb = goingActive ? "reactivate" : "deactivate";
        var confirm = MessageBox.Show(this, $"Are you sure you want to {verb} {_existing.ProductName}?",
            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            await _service.SetActiveStatusAsync(_existing.ProductId, goingActive);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Unable to update the product's status. Please try again.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
