using DartERP.Application.Services;
using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.WinForms.Forms;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Controls;

public class DispositionListControl : UserControl
{
    private record DispositionRow(string SerialNumber, string Product, string DispositionDate, string Type, string Recipient, string Notes);

    private readonly DispositionService _service;
    private readonly SerializedItemService _serializedItemService;
    private readonly CustomerService _customerService;
    private readonly SfDataGrid _grid;
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

        var exportButton = new Button { Text = "Export CSV", Width = 110, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        exportButton.Click += (_, _) => CsvExporter.ExportGrid(_grid, "ADLog.csv");

        toolbar.Controls.Add(titleLabel);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(newButton);

        _grid = new SfDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        _grid.StyleAsSfDataGrid();
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
        _grid.Columns.Add(new GridTextColumn { MappingName = "SerialNumber", HeaderText = "Serial Number", Width = 140 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Product", HeaderText = "Product", Width = 150 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "DispositionDate", HeaderText = "Date", Width = 90 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Type", HeaderText = "Type", Width = 100 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Recipient", HeaderText = "Recipient", Width = 170 });
        _grid.Columns.Add(new GridTextColumn { MappingName = "Notes", HeaderText = "Notes", Width = 180 });
    }

    private async Task RefreshAsync()
    {
        var results = await _service.GetAllWithDetailsAsync();

        // Navigating away disposes this control while the query above is
        // still in flight — SfDataGrid throws on a DataSource assignment
        // after disposal (DataGridView never did), so this guard is load-bearing.
        if (IsDisposed)
            return;

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
            _grid.DataSource = results.Select(d => new DispositionRow(
                d.SerializedItem?.SerialNumber ?? string.Empty, d.SerializedItem?.Product?.ProductName ?? string.Empty,
                d.DispositionDate.ToString("MM/dd/yyyy"), EnumDisplay.For(d.Type), d.Customer?.CompanyName ?? "-", d.Notes)).ToList();
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
