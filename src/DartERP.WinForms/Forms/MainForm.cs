using System.Diagnostics;
using DartERP.Application;
using DartERP.Application.Services;
using DartERP.Core.Interfaces;
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
    private readonly CurrentUserContext _currentUserContext;
    private readonly UserService _userService;
    private readonly Dictionary<string, Func<Control>> _moduleFactories;
    private readonly List<NavButton> _navButtons = [];

    private Panel? _sidebar;
    private Panel? _rightSide;
    private Panel _contentPanel = null!;
    private LetterSpacedLabel _pageTitleLabel = null!;
    private Avatar _headerAvatar = null!;
    private Label _headerNameLabel = null!;
    private Control? _activeModule;
    private string _currentModuleName = "Dashboard";

    /// <summary>
    /// True only when the window closed because the user chose Log Out from
    /// the account menu — Program.cs checks this to decide whether to loop
    /// back to the login screen or exit the app for good. A plain window
    /// close (the X button) leaves this false.
    /// </summary>
    public bool LoggedOut { get; private set; }

    public MainForm(IServiceProvider serviceProvider, CurrentUserContext currentUserContext, UserService userService)
    {
        _serviceProvider = serviceProvider;
        _currentUserContext = currentUserContext;
        _userService = userService;

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

        _pageTitleLabel = new LetterSpacedLabel
        {
            Text = _currentModuleName,
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var accountCluster = BuildAccountCluster(headerBar.Height);

        headerBar.Controls.Add(_pageTitleLabel);
        headerBar.Controls.Add(accountCluster);
        headerBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderColor);
            e.Graphics.DrawLine(pen, 0, headerBar.Height - 1, headerBar.Width, headerBar.Height - 1);
        };

        return headerBar;
    }

    private Panel BuildAccountCluster(int headerHeight)
    {
        const int clusterWidth = 220;
        const int avatarSize = 32;

        var cluster = new Panel { Dock = DockStyle.Right, Width = clusterWidth, Cursor = Cursors.Hand };
        var avatarY = (headerHeight - avatarSize) / 2;

        _headerAvatar = new Avatar { Width = avatarSize, Height = avatarSize, Location = new Point(clusterWidth - avatarSize - 24, avatarY) };
        _headerNameLabel = new Label
        {
            AutoSize = false,
            Width = _headerAvatar.Left - 16,
            Height = avatarSize,
            Location = new Point(0, avatarY),
            Font = Theme.FontBodyBold,
            ForeColor = Theme.TextPrimary,
            TextAlign = ContentAlignment.MiddleRight,
        };
        RefreshHeaderIdentity();

        cluster.Controls.Add(_headerAvatar);
        cluster.Controls.Add(_headerNameLabel);

        var menu = BuildAccountMenu();
        void OpenMenu(object? sender, EventArgs e) => menu.Show(cluster, new Point(clusterWidth - 170, headerHeight));
        cluster.Click += OpenMenu;
        _headerAvatar.Click += OpenMenu;
        _headerNameLabel.Click += OpenMenu;

        return cluster;
    }

    private ContextMenuStrip BuildAccountMenu()
    {
        var menu = new ContextMenuStrip { Font = Theme.FontBody };

        var profileItem = menu.Items.Add("Profile");
        profileItem.Click += (_, _) => OpenProfile();

        var lockItem = menu.Items.Add("Lock");
        lockItem.Click += (_, _) => OpenLock();

        menu.Items.Add(new ToolStripSeparator());

        var logoutItem = menu.Items.Add("Log Out");
        logoutItem.Click += (_, _) => LogOut();

        return menu;
    }

    private void RefreshHeaderIdentity()
    {
        var user = _currentUserContext.CurrentUser!;
        _headerNameLabel.Text = user.DisplayName;
        _headerAvatar.SetUser(user.DisplayName, user.Username, ProfilePictureStore.Load(user.ProfilePicturePath));
    }

    private void OpenProfile()
    {
        using var form = new ProfileForm(_userService, _currentUserContext);
        form.ProfileUpdated += (_, _) => RefreshHeaderIdentity();
        form.ShowDialog(this);
    }

    private void OpenLock()
    {
        using var form = new LockForm(_userService, _currentUserContext.CurrentUser!);
        form.ShowDialog(this);
    }

    private void LogOut()
    {
        LoggedOut = true;
        Close();
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
            Height = 96,
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
        var logoHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 20) };
        logoHost.Controls.Add(logoBox);

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
        ["Database"] = () => new DatabaseExplorerControl(
            _serviceProvider.GetRequiredService<ICustomerRepository>(),
            _serviceProvider.GetRequiredService<IVendorRepository>(),
            _serviceProvider.GetRequiredService<IProductRepository>(),
            _serviceProvider.GetRequiredService<IPurchaseOrderRepository>(),
            _serviceProvider.GetRequiredService<IWorkOrderRepository>(),
            _serviceProvider.GetRequiredService<ISerializedItemRepository>(),
            _serviceProvider.GetRequiredService<IQualityInspectionRepository>(),
            _serviceProvider.GetRequiredService<IDispositionRepository>(),
            _serviceProvider.GetRequiredService<IUserRepository>()),
        ["Settings"] = () => new SettingsControl(),
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
