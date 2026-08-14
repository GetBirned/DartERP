using System.Diagnostics;
using DartERP.Application.Services;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace DartERP.WinForms.Forms;

/// <summary>
/// Application shell: left navigation sidebar + a content panel that hosts
/// whichever module control is currently active.
/// </summary>
public class MainForm : Form
{
    private const string RepoUrl = "https://github.com/GetBirned/DartERP";

    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Func<Control>> _moduleFactories;
    private readonly List<NavButton> _navButtons = [];

    private Panel? _sidebar;
    private Panel? _rightSide;
    private Panel _contentPanel = null!;
    private Label _pageTitleLabel = null!;
    private Control? _activeModule;
    private string _currentModuleName = "Dashboard";

    public MainForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        Text = "DartERP - DERP Manufacturing System";
        MinimumSize = new Size(1180, 720);
        Size = new Size(1360, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = AppAssets.WindowIcon;

        _moduleFactories = BuildModuleFactories();

        // Theming is live: flipping Theme.CurrentMode fires this, and we
        // rebuild the whole shell (plus whichever module is on screen) from
        // scratch rather than hunting down every control's color property.
        Theme.ThemeChanged += (_, _) => RebuildShell();

        BuildShell();
        NavigateTo("Dashboard");
    }

    private void RebuildShell()
    {
        _sidebar?.Dispose();
        _rightSide?.Dispose();
        Controls.Clear();
        _navButtons.Clear();

        BuildShell();
        NavigateTo(_currentModuleName);
    }

    private void BuildShell()
    {
        BackColor = Theme.AppBackground;
        Font = Theme.FontBody;

        _sidebar = BuildSidebar();
        var headerBar = BuildHeaderBar();

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.AppBackground,
            Padding = new Padding(24),
        };

        _rightSide = new Panel { Dock = DockStyle.Fill };
        _rightSide.Controls.Add(_contentPanel);
        _rightSide.Controls.Add(headerBar);

        Controls.Add(_rightSide);
        Controls.Add(_sidebar);
    }

    private Panel BuildHeaderBar()
    {
        var headerBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Theme.CardBackground,
        };

        _pageTitleLabel = new Label
        {
            Text = _currentModuleName,
            UseMnemonic = false, // module names like "A&D Log" shouldn't treat & as an accelerator prefix
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var themeToggle = BuildThemeToggle();

        headerBar.Controls.Add(_pageTitleLabel);
        headerBar.Controls.Add(themeToggle);
        headerBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderColor);
            e.Graphics.DrawLine(pen, 0, headerBar.Height - 1, headerBar.Width, headerBar.Height - 1);
        };

        return headerBar;
    }

    private Button BuildThemeToggle()
    {
        var isDark = Theme.CurrentMode == ThemeMode.Dark;
        var toggle = new Button
        {
            Text = isDark ? "Light Mode" : "Dark Mode",
            Width = 110,
            Dock = DockStyle.Right,
            Margin = new Padding(0, 11, 24, 11),
        }.StyleAsSecondaryButton();

        toggle.Click += (_, _) =>
        {
            var newMode = Theme.CurrentMode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
            var preferences = AppPreferences.Load();
            preferences.Theme = newMode;
            preferences.Save();
            Theme.CurrentMode = newMode;
        };

        return toggle;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = Theme.SidebarBackground,
        };

        var brandPanel = BuildBrandPanel();
        var footer = BuildSidebarFooter();
        var navPanel = new Panel { Dock = DockStyle.Fill };

        // Added in reverse since each button docks to Top of navPanel.
        var moduleNames = _moduleFactories.Keys.Reverse();
        foreach (var name in moduleNames)
        {
            var button = new NavButton(name) { IsSelected = name == _currentModuleName };
            button.NavClicked += (_, _) => NavigateTo(name);
            navPanel.Controls.Add(button);
            _navButtons.Add(button);
        }
        _navButtons.Reverse();

        sidebar.Controls.Add(navPanel);
        sidebar.Controls.Add(footer);
        sidebar.Controls.Add(brandPanel);

        return sidebar;
    }

    private static Panel BuildBrandPanel()
    {
        var brandPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 106,
            BackColor = Theme.SidebarBackground,
        };

        // PictureBox ignores Margin when docked, so the inset comes from a
        // wrapping panel's Padding instead.
        var logoBox = new PictureBox
        {
            Image = AppAssets.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
        };
        var logoHost = new Panel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(24, 20, 24, 0) };
        logoHost.Controls.Add(logoBox);

        var subLabel = new Label
        {
            Text = "DERP Manufacturing System",
            Font = Theme.FontSmall,
            ForeColor = Theme.SidebarText,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(24, 4, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        brandPanel.Controls.Add(subLabel);
        brandPanel.Controls.Add(logoHost);

        return brandPanel;
    }

    private static Panel BuildSidebarFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Theme.SidebarBackground,
        };

        var sourceLink = new LinkLabel
        {
            Text = "Source Code",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            LinkColor = Theme.SidebarText,
            ActiveLinkColor = Theme.SidebarSelected,
            VisitedLinkColor = Theme.SidebarText,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Font = Theme.FontSmall,
        };
        sourceLink.LinkClicked += (_, _) =>
        {
            Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true });
        };

        footer.Controls.Add(sourceLink);
        return footer;
    }

    private Dictionary<string, Func<Control>> BuildModuleFactories() => new()
    {
        ["Dashboard"] = () => new DashboardControl(_serviceProvider.GetRequiredService<DashboardService>()),
        ["Customers"] = () => new CustomerListControl(_serviceProvider.GetRequiredService<CustomerService>()),
        ["Vendors"] = () => new VendorListControl(_serviceProvider.GetRequiredService<VendorService>()),
        ["Products"] = () => new ProductListControl(_serviceProvider.GetRequiredService<ProductService>()),
        ["Inventory"] = () => new InventoryControl(_serviceProvider.GetRequiredService<InventoryService>()),
        ["Purchase Orders"] = () => new PurchaseOrderListControl(
            _serviceProvider.GetRequiredService<PurchaseOrderService>(),
            _serviceProvider.GetRequiredService<VendorService>(),
            _serviceProvider.GetRequiredService<ProductService>()),
        ["Work Orders"] = () => new WorkOrderListControl(
            _serviceProvider.GetRequiredService<WorkOrderService>(),
            _serviceProvider.GetRequiredService<ProductService>()),
        ["Serialized Inventory"] = () => new SerializedItemListControl(
            _serviceProvider.GetRequiredService<SerializedItemService>(),
            _serviceProvider.GetRequiredService<WorkOrderService>()),
        ["Quality Control"] = () => new QualityControlListControl(
            _serviceProvider.GetRequiredService<QualityInspectionService>(),
            _serviceProvider.GetRequiredService<SerializedItemService>()),
        ["A&D Log"] = () => new DispositionListControl(
            _serviceProvider.GetRequiredService<DispositionService>(),
            _serviceProvider.GetRequiredService<SerializedItemService>(),
            _serviceProvider.GetRequiredService<CustomerService>()),
    };

    public void NavigateTo(string moduleName)
    {
        if (!_moduleFactories.TryGetValue(moduleName, out var factory))
            return;

        _currentModuleName = moduleName;

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
