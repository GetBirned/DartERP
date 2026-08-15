using System.Collections;
using DartERP.Core.Interfaces;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// A generic, read-only table browser. Every other grid in this app defines
/// its columns by hand and goes through Application.Services — this one
/// deliberately does neither. It binds straight to whichever repository's
/// plain GetAllAsync() the user picks and lets DataGridView's
/// AutoGenerateColumns build columns via reflection, because the whole
/// point is showing the actual database contents, not a curated business
/// view of them. Going around the service layer is on purpose too: most
/// services only expose filtered/shaped queries for their own screens
/// (CustomerService.SearchAsync, for instance, has no "every row, no
/// filter" method), but IRepository&lt;T&gt;.GetAllAsync() is the one thing
/// every entity has in common.
/// </summary>
public class DatabaseExplorerControl : UserControl
{
    private readonly Dictionary<string, Func<Task<IList>>> _loaders;
    private readonly ListBox _tableList;
    private readonly DataGridView _grid;
    private readonly Label _rowCountLabel;

    private IList _currentRows = Array.Empty<object>();
    private string? _sortProperty;
    private bool _sortAscending = true;

    public DatabaseExplorerControl(
        ICustomerRepository customerRepository,
        IVendorRepository vendorRepository,
        IProductRepository productRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IWorkOrderRepository workOrderRepository,
        ISerializedItemRepository serializedItemRepository,
        IQualityInspectionRepository qualityInspectionRepository,
        IDispositionRepository dispositionRepository,
        IUserRepository userRepository)
    {
        // Table names, not friendly labels — this screen is meant to read
        // as "the real schema," not another polished business view, so it
        // matches docs/DATABASE.md exactly. PurchaseOrderLines is the one
        // table left out: it has no repository of its own (lines are only
        // ever touched through the PurchaseOrder aggregate), and adding one
        // just for this screen would break the "zero new data access" point
        // of the whole thing.
        _loaders = new Dictionary<string, Func<Task<IList>>>
        {
            ["Customers"] = async () => (IList)await customerRepository.GetAllAsync(),
            ["Vendors"] = async () => (IList)await vendorRepository.GetAllAsync(),
            ["Products"] = async () => (IList)await productRepository.GetAllAsync(),
            ["PurchaseOrders"] = async () => (IList)await purchaseOrderRepository.GetAllAsync(),
            ["PurchaseOrderStatusHistories"] = async () => (IList)await purchaseOrderRepository.GetAllStatusHistoryAsync(),
            ["PurchaseOrderAttachments"] = async () => (IList)await purchaseOrderRepository.GetAllAttachmentsAsync(),
            ["WorkOrders"] = async () => (IList)await workOrderRepository.GetAllAsync(),
            ["WorkOrderStatusHistories"] = async () => (IList)await workOrderRepository.GetAllStatusHistoryAsync(),
            ["SerializedItems"] = async () => (IList)await serializedItemRepository.GetAllAsync(),
            ["QualityInspections"] = async () => (IList)await qualityInspectionRepository.GetAllAsync(),
            ["Dispositions"] = async () => (IList)await dispositionRepository.GetAllAsync(),
            ["Users"] = async () => (IList)await userRepository.GetAllAsync(),
        };

        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var subtitle = new Label
        {
            Text = "Read-only view of the underlying database tables. Click a column header to sort.",
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 32,
        };

        var listPanel = new Panel { Dock = DockStyle.Left, Width = 190, BackColor = Theme.CardBackground };
        _tableList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = Theme.FontBody,
            BackColor = Theme.CardBackground,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 34,
            IntegralHeight = false,
        };
        _tableList.Items.AddRange(_loaders.Keys.Cast<object>().ToArray());
        _tableList.DrawItem += TableList_DrawItem;
        _tableList.SelectedIndexChanged += async (_, _) => await LoadSelectedAsync();
        listPanel.Controls.Add(_tableList);

        _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        _grid.StyleAsDataGrid();
        _grid.ColumnHeaderMouseClick += (_, e) => SortByColumnIndex(e.ColumnIndex);
        _grid.DataBindingComplete += (_, _) => HideComplexColumns();

        _rowCountLabel = new Label
        {
            Text = string.Empty,
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(4, 4, 0, 0),
        };

        var gridPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground };
        gridPanel.Controls.Add(_grid);
        gridPanel.Controls.Add(_rowCountLabel);

        var separator = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.BorderColor };

        Controls.Add(gridPanel);
        Controls.Add(separator);
        Controls.Add(listPanel);
        Controls.Add(subtitle);

        Load += (_, _) => _tableList.SelectedIndex = 0;
    }

    private void TableList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
            return;

        var text = _tableList.Items[e.Index].ToString() ?? string.Empty;
        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using (var brush = new SolidBrush(isSelected ? Theme.SelectionHighlight : Theme.CardBackground))
            e.Graphics.FillRectangle(brush, e.Bounds);

        var textRect = new Rectangle(e.Bounds.X + 20, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, Theme.FontBody, textRect, Theme.TextPrimary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private async Task LoadSelectedAsync()
    {
        if (_tableList.SelectedItem is not string tableName || !_loaders.TryGetValue(tableName, out var loader))
            return;

        _sortProperty = null;
        _currentRows = await loader();

        _grid.DataSource = null;
        _grid.DataSource = _currentRows;
        _rowCountLabel.Text = $"{_currentRows.Count} row{(_currentRows.Count == 1 ? "" : "s")}";
    }

    /// <summary>
    /// AutoGenerateColumns builds a column for every public property,
    /// navigation properties included — those come back null or an empty
    /// collection from a plain GetAllAsync() (nothing here calls .Include),
    /// so instead of a column full of blanks or "System.Collections.Generic
    /// .List`1[...]" text, this hides anything that isn't a plain scalar
    /// value. Driven by the column's ValueType, so it's still one generic
    /// rule applied the same way to every table, not a per-entity list.
    /// </summary>
    private void HideComplexColumns()
    {
        foreach (DataGridViewColumn column in _grid.Columns)
            column.Visible = IsSimpleType(column.ValueType);
    }

    private static bool IsSimpleType(Type? type)
    {
        if (type is null)
            return false;

        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum
            || underlying == typeof(string) || underlying == typeof(decimal)
            || underlying == typeof(DateTime) || underlying == typeof(Guid);
    }

    private void SortByColumnIndex(int columnIndex)
    {
        if (_currentRows.Count == 0)
            return;

        var propertyName = _grid.Columns[columnIndex].DataPropertyName;
        var elementType = _currentRows[0]!.GetType();
        var property = elementType.GetProperty(propertyName);
        if (property is null)
            return;

        _sortAscending = _sortProperty == propertyName ? !_sortAscending : true;
        _sortProperty = propertyName;

        var sorted = _currentRows.Cast<object>()
            .OrderBy(item => property.GetValue(item), Comparer<object?>.Create(CompareForSort))
            .ToList();
        if (!_sortAscending)
            sorted.Reverse();

        // Rebuild as a concretely-typed List<T> (via reflection on the
        // element type) rather than handing DataGridView a List<object> —
        // AutoGenerateColumns reads the bound list's declared element type,
        // and List<object> would make every column show up as "object".
        var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (var item in sorted)
            typedList.Add(item);

        _currentRows = typedList;
        _grid.DataSource = null;
        _grid.DataSource = _currentRows;
    }

    private static int CompareForSort(object? a, object? b)
    {
        if (a is null && b is null)
            return 0;
        if (a is null)
            return -1;
        if (b is null)
            return 1;

        return a is IComparable comparable
            ? comparable.CompareTo(b)
            : string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }
}
