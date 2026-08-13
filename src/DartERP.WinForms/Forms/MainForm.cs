using DartERP.Application.Services;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace DartERP.WinForms.Forms;

/// <summary>
/// Application shell: left navigation sidebar + a content panel that hosts
/// whichever module control is currently active.
/// </summary>
public class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Panel _contentPanel;
    private readonly Label _pageTitleLabel;
    private readonly List<NavButton> _navButtons = [];
    private readonly Dictionary<string, Func<Control>> _moduleFactories;
    private Control? _activeModule;

    public MainForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        Text = "DartERP - DERP Manufacturing System";
        MinimumSize = new Size(1180, 720);
        Size = new Size(1360, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.AppBackground;
        Font = Theme.FontBody;

        _moduleFactories = BuildModuleFactories();

        var sidebar = BuildSidebar();
        var headerBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Theme.CardBackground,
        };
        _pageTitleLabel = new Label
        {
            Text = "Dashboard",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        headerBar.Controls.Add(_pageTitleLabel);
        headerBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderColor);
            e.Graphics.DrawLine(pen, 0, headerBar.Height - 1, headerBar.Width, headerBar.Height - 1);
        };

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.AppBackground,
            Padding = new Padding(24),
        };

        var rightSide = new Panel { Dock = DockStyle.Fill };
        rightSide.Controls.Add(_contentPanel);
        rightSide.Controls.Add(headerBar);

        Controls.Add(rightSide);
        Controls.Add(sidebar);

        NavigateTo("Dashboard");
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = Theme.SidebarBackground,
        };

        var brandPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = Theme.SidebarBackground,
        };
        var subLabel = new Label
        {
            Text = "DERP Manufacturing System",
            Font = Theme.FontSmall,
            ForeColor = Theme.SidebarText,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(24, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var brandLabel = new Label
        {
            Text = "DARTERP",
            Font = Theme.FontBrand,
            ForeColor = Color.White,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(24, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var topSpacer = new Panel { Dock = DockStyle.Top, Height = 18 };
        brandPanel.Controls.Add(subLabel);
        brandPanel.Controls.Add(brandLabel);
        brandPanel.Controls.Add(topSpacer);

        var navPanel = new Panel { Dock = DockStyle.Fill };

        // Added in reverse since each button docks to Top of navPanel.
        var moduleNames = _moduleFactories.Keys.Reverse();
        foreach (var name in moduleNames)
        {
            var button = new NavButton(name);
            button.NavClicked += (_, _) => NavigateTo(name);
            navPanel.Controls.Add(button);
            _navButtons.Add(button);
        }
        _navButtons.Reverse();

        sidebar.Controls.Add(navPanel);
        sidebar.Controls.Add(brandPanel);

        return sidebar;
    }

    private Dictionary<string, Func<Control>> BuildModuleFactories() => new()
    {
        ["Dashboard"] = () => Placeholder("Dashboard"),
        ["Customers"] = () => new CustomerListControl(_serviceProvider.GetRequiredService<CustomerService>()),
        ["Vendors"] = () => new VendorListControl(_serviceProvider.GetRequiredService<VendorService>()),
        ["Products"] = () => new ProductListControl(_serviceProvider.GetRequiredService<ProductService>()),
        ["Inventory"] = () => new InventoryControl(_serviceProvider.GetRequiredService<InventoryService>()),
        ["Purchase Orders"] = () => new PurchaseOrderListControl(
            _serviceProvider.GetRequiredService<PurchaseOrderService>(),
            _serviceProvider.GetRequiredService<VendorService>(),
            _serviceProvider.GetRequiredService<ProductService>()),
        ["Work Orders"] = () => Placeholder("Work Orders"),
        ["Quality Control"] = () => Placeholder("Quality Control"),
    };

    private static Control Placeholder(string moduleName) =>
        new EmptyStateControl(moduleName, "This module is under construction.");

    public void NavigateTo(string moduleName)
    {
        if (!_moduleFactories.TryGetValue(moduleName, out var factory))
            return;

        foreach (var button in _navButtons)
            button.IsSelected = button.Text == moduleName;

        _pageTitleLabel.Text = moduleName;

        if (_activeModule is not null)
        {
            _contentPanel.Controls.Remove(_activeModule);
            if (_activeModule is IDisposable disposable)
                disposable.Dispose();
        }

        _activeModule = factory();
        _activeModule.Dock = DockStyle.Fill;
        _contentPanel.Controls.Add(_activeModule);
    }
}
