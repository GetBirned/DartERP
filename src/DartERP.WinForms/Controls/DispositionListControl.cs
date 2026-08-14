using DartERP.Application.Services;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public class DispositionListControl : UserControl
{
    private readonly DispositionService _service;
    private readonly SerializedItemService _serializedItemService;
    private readonly CustomerService _customerService;
    private readonly DataGridView _grid;
    private readonly Panel _gridHost;
    private EmptyStateControl? _emptyState;

    public DispositionListControl(DispositionService service, SerializedItemService serializedItemService, CustomerService customerService)
    {
        _service = service;
        _serializedItemService = serializedItemService;
        _customerService = customerService;
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        var newButton = new Button { Text = "+ Record Disposition", Width = 180, Dock = DockStyle.Right }.StyleAsPrimaryButton();
        newButton.Click += async (_, _) => await OpenEditorAsync();

        var titleLabel = new Label
        {
            Text = "Acquisition & Disposition Log — a permanent record of every serialized item's disposition",
            UseMnemonic = false,
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

        _gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground };
        _gridHost.Controls.Add(_grid);

        Controls.Add(_gridHost);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
        Controls.Add(toolbar);

        Load += async (_, _) => await RefreshAsync();
    }

    private void BuildColumns()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "Serial Number", FillWeight = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product", FillWeight = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DispositionDate", HeaderText = "Date", FillWeight = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", FillWeight = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Recipient", HeaderText = "Recipient", FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes", FillWeight = 180 });

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;

            var disposition = (Disposition)_grid.Rows[e.RowIndex].DataBoundItem;
            var columnName = _grid.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "SerialNumber":
                    e.Value = disposition.SerializedItem?.SerialNumber ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "Product":
                    e.Value = disposition.SerializedItem?.Product?.ProductName ?? string.Empty;
                    e.FormattingApplied = true;
                    break;
                case "DispositionDate":
                    e.Value = disposition.DispositionDate.ToString("MM/dd/yyyy");
                    e.FormattingApplied = true;
                    break;
                case "Type":
                    e.Value = EnumDisplay.For(disposition.Type);
                    e.FormattingApplied = true;
                    break;
                case "Recipient":
                    e.Value = disposition.Customer?.CompanyName ?? "-";
                    e.FormattingApplied = true;
                    break;
            }
        };
    }

    private async Task RefreshAsync()
    {
        var results = await _service.GetAllWithDetailsAsync();

        if (results.Count == 0)
        {
            _grid.Visible = false;
            _emptyState ??= new EmptyStateControl("No dispositions recorded yet", "Record a disposition for an in-stock serialized item to get started.");
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

    private async Task OpenEditorAsync()
    {
        var allItems = await _serializedItemService.GetAllWithDetailsAsync();
        var availableItems = allItems.Where(i => i.Status == SerializedItemStatus.InStock).ToList();

        if (availableItems.Count == 0)
        {
            MessageBox.Show(FindForm(), "No serialized items are currently In Stock and available to dispose.",
                "Nothing available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var activeCustomers = await _customerService.SearchAsync(null, activeOnly: true);

        using var form = new DispositionEditForm(_service, availableItems, activeCustomers);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await RefreshAsync();
    }
}
