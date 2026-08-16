using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// A look-up-a-term glossary of the technologies this project actually uses,
/// written in first person — the point is explaining the thought process
/// behind the build, not just naming the stack. Structurally this is
/// DatabaseExplorerControl's left-list/right-detail split reused for prose
/// instead of a data grid.
/// </summary>
public class TechGlossaryControl : UserControl
{
    private readonly ListBox _termList;
    private readonly Panel _detailPanel;

    private record GlossaryEntry(string Term, string Category, string Summary, string Explanation, Func<Control>? BuildDiagram = null);

    // Ordered to tell the build story in sequence, not alphabetically —
    // language/runtime, then architecture, then data access, then security,
    // then UI, then testing, then tooling.
    private static readonly List<GlossaryEntry> Entries =
    [
        new(
            "C# & .NET 8",
            "Language & Runtime",
            "The language and runtime the whole solution runs on.",
            "DartERP runs on .NET 8 with C# from top to bottom. Every project targets net8.0, except the WinForms host, which targets net8.0-windows since it needs access to the Windows Forms APIs. Nullable reference types are enabled everywhere, so the compiler flags a missing null check before I ever run the app. I used newer C# features on purpose instead of falling back on older habits: records for DTOs like PurchaseOrderLineInput, pattern matching for status checks, collection expressions like [] for empty line lists, and primary constructors where they cut down on boilerplate."),
        new(
            "WinForms",
            "UI Framework",
            "The desktop framework I chose on purpose, not by default.",
            "I picked WinForms over a web stack because I wanted this to feel like a real internal business app, which is still how a lot of manufacturing ERP software actually ships. The catch is WinForms gives you almost nothing for free visually. No CSS, no built-in theming, no charting. So I built my own styling layer on top of it: Theme.cs defines a light and dark color palette, ControlStyleExtensions gives every button, grid, and text input consistent styling through extension methods like StyleAsPrimaryButton and StyleAsDataGrid, and hand-drawn GDI+ controls cover anything the stock toolbox couldn't do, like the dashboard's donut chart. I wasn't going to ship the default gray-button look."),
        new(
            "Layered Architecture",
            "Architecture",
            "Four projects, one dependency direction, enforced by the compiler.",
            "I split the solution into DartERP.Core (models, enums, interfaces, zero dependencies), DartERP.Application and DartERP.Infrastructure (both depending only on Core), and DartERP.WinForms (depending on all three). A zero-dependency Core forces the dependency arrows to only point one way. If a repository in Infrastructure ever tried to reference a form in WinForms, that's a compile error, not something I'd have to catch in review. WinForms never touches the DbContext directly either. A screen like PurchaseOrderListControl only ever talks to a PurchaseOrderService, which only ever talks to an IPurchaseOrderRepository interface defined in Core. That's what keeps the UI swappable in theory, even though I never built a second one to prove it.",
            () => new LayeredArchitectureDiagram()),
        new(
            "Dependency Injection",
            "Architecture",
            "One composition root. Everything gets wired in Program.cs.",
            "Every service, repository, and form gets registered with Microsoft.Extensions.DependencyInjection in Program.cs. A form like PurchaseOrderListControl never news up a PurchaseOrderService itself; it gets one handed to it through its constructor. Almost everything is registered Singleton, since WinForms has no per-request scope and the repositories don't hold any state of their own. MainForm and LoginForm are the two exceptions, registered Transient, because logging out needs a genuinely fresh MainForm with a clean header identity, and a fresh LoginForm, every time the sign-in loop comes back around."),
        new(
            "Repository Pattern",
            "Architecture",
            "One interface per entity, plus a shared base for the methods they all need.",
            "Every entity gets its own interface, like IPurchaseOrderRepository, extending a small shared IRepository<T> that defines GetByIdAsync, GetAllAsync, AddAsync, and UpdateAsync, plus whatever real query methods that entity actually needs, like GetBelowReorderLevelAsync on the product repository. I didn't build a fully generic Repository<T> framework on top of that on purpose. It would've added abstraction without saving real code, since almost every screen needs a query the generic base can't express on its own. Each repository method opens its own short-lived DbContext through an IDbContextFactory instead of holding one long-lived context, since a WinForms app has no natural request boundary to tie a context's lifetime to.",
            () => new DataFlowDiagram()),
        new(
            "Entity Framework Core",
            "Data Access",
            "Code-first, with migrations and Fluent API config for every entity.",
            "The schema is defined entirely in C#. Every entity gets its own Configuration.cs file under Infrastructure's Configurations folder, implementing IEntityTypeConfiguration<T>. That covers unique indexes, decimal(18,2) precision on money columns, and enum-to-string conversions so a raw query in SSMS shows 'Submitted' instead of a plain integer. Every schema change goes through dotnet ef migrations add, and migrations apply automatically at startup with Database.Migrate(), so there's no manual setup step after a fresh clone. I also put real thought into delete behavior per relationship: cascade only for true parent and child pairs, like a PurchaseOrder and its lines, and restrict everywhere else, since most 'deletes' in this app are actually soft deletes through an IsActive flag. An accidental cascade should be impossible by construction, not just by convention."),
        new(
            "SQL Server (LocalDB)",
            "Data Access",
            "A real relational database, running locally, with nothing to configure.",
            "I used SQL Server LocalDB for development so the whole thing runs on a fresh clone with zero setup. No Docker, no cloud connection string, nobody has to ask me for credentials. The connection string lives in appsettings.json with nothing sensitive in it, and a small DartErpDbContextFactory class implementing IDesignTimeDbContextFactory<DartErpDbContext> lets the dotnet ef CLI build the context at design time to generate a migration, without spinning up the whole DI container just for that."),
        new(
            "LINQ",
            "Data Access",
            "Queries written as C#, translated to real SQL by EF Core.",
            "The dashboard's KPIs and attention-needed lists are LINQ queries EF Core translates to real SQL on the server. Where(p => p.QuantityOnHand <= p.ReorderLevel) finds low-stock products, and SumAsync adds up units currently in production, without pulling every row into memory first. The two chart aggregations, purchase orders by status and inventory value by category, use GroupBy followed by ToDictionaryAsync in the repositories. That's the natural LINQ shape for counting or summing grouped by a column, and a lot less code than writing the equivalent by hand in raw SQL."),
        new(
            "Async & Await",
            "Language & Runtime",
            "Every data call is async, all the way up to the click handler.",
            "Every repository and service method returns a Task, and every button click or form Load event that touches data is an async lambda, so the UI thread never sits and waits on a database round trip. DashboardService.GetSummaryAsync fires around a dozen of these queries at once with Task.WhenAll, which only works because each query opens its own DbContext through the factory. If they shared one context, running them concurrently would throw, since a DbContext isn't thread-safe."),
        new(
            "PBKDF2 Password Hashing",
            "Security",
            "Passwords get hashed, not encrypted, and that distinction matters.",
            "I used Rfc2898DeriveBytes.Pbkdf2, built right into .NET's System.Security.Cryptography, so there's no third-party crypto package involved. It runs 100,000 iterations with a random 16-byte salt per user and SHA-256 as the hash algorithm. Hashing is one-way on purpose. There's no key that turns a hash back into the original password, unlike encryption, which is reversible and just the wrong tool for this job even though the two get confused. Verification uses CryptographicOperations.FixedTimeEquals instead of a plain comparison, so a timing attack can't be used to guess the stored hash a byte at a time. It costs nothing to do that correctly, so there's no reason not to."),
        new(
            "WebView2",
            "UI Framework",
            "A real browser engine, embedded, just to run the login video.",
            "The sign-in screen has a looping video background, and WinForms has no good native way to decode and loop video smoothly. So instead of fighting raw GDI+ video decoding, I embedded a WebView2 control that navigates to a small local login.html file with a looping video tag. If the WebView2 Runtime isn't installed on the machine, LoginForm falls back to a static branded panel instead, so a missing runtime degrades gracefully rather than crashing the app before anyone can even sign in."),
        new(
            "GDI+ Custom Drawing",
            "UI Framework",
            "No charting library, no icon library. Every visual is hand-drawn.",
            "The dashboard's PieChart and BarChart controls, every sidebar icon in NavIconRenderer, the rounded card corners, the status badges, even the two diagrams on this exact screen, are all plain GDI+: Graphics.FillPath, DrawPath, and DrawString calls inside OnPaint overrides, not a NuGet charting package. I made that call because the built-in charting control looks dated, and a third-party charting library means fighting its own theming API just to hit this app's exact brand tan, #D4C6A6. Hand-drawing gives me pixel-exact color control for free, and once I built one shared DashboardCard base class for the rounded-card chrome, it wasn't actually that much code to reuse across PieChart, BarChart, and DashboardListCard."),
        new(
            "xUnit & Fake Repositories",
            "Testing",
            "44 tests, in-memory fakes, no real database involved.",
            "The test suite runs against hand-written in-memory fake repositories, like FakePurchaseOrderRepository, that implement the exact same interfaces the real EF Core repositories do, instead of hitting LocalDB. That's what keeps the whole suite running in about a tenth of a second, and makes it safe to run constantly instead of only once in a while. The tests target actual business rules: purchase order validation, unique serial number generation, work order date ordering, password hashing round-trips, and status history logging firing on real transitions while staying quiet on a no-op save. That's logic worth protecting, not incidental plumbing."),
        new(
            "Git & GitHub",
            "Tooling",
            "Version control as the paper trail, and the repo itself as part of the deliverable.",
            "Every feature went in as its own commit, with a message explaining why, not just what changed, like the commit that added the audit trail or the one that fixed the avatar color changing on every restart. The whole history is public on GitHub, so the commit log is part of what I'd want someone to actually look at, not just the final diff. I committed in feature-sized chunks on purpose instead of one giant initial commit, since a reviewable history was part of the point of building this as a portfolio piece in the first place."),
    ];

    public TechGlossaryControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var subtitle = new Label
        {
            Text = "What I used, and why — click a term to read how it fits into this project.",
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 32,
        };

        var listPanel = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.CardBackground };
        _termList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = Theme.FontBody,
            BackColor = Theme.CardBackground,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 46,
            IntegralHeight = false,
        };
        _termList.Items.AddRange(Entries.Select(e => e.Term).Cast<object>().ToArray());
        _termList.DrawItem += TermList_DrawItem;
        _termList.SelectedIndexChanged += (_, _) => ShowSelected();
        listPanel.Controls.Add(_termList);

        var separator = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.BorderColor };

        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBackground,
            AutoScroll = true,
            Padding = new Padding(32, 24, 32, 24),
        };
        _detailPanel.Resize += (_, _) =>
        {
            if (_termList.SelectedIndex >= 0)
                ShowSelected();
        };

        Controls.Add(_detailPanel);
        Controls.Add(separator);
        Controls.Add(listPanel);
        Controls.Add(subtitle);

        Load += (_, _) => _termList.SelectedIndex = 0;
    }

    private void TermList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
            return;

        var entry = Entries[e.Index];
        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using (var brush = new SolidBrush(isSelected ? Theme.SelectionHighlight : Theme.CardBackground))
            e.Graphics.FillRectangle(brush, e.Bounds);

        var termRect = new Rectangle(e.Bounds.X + 16, e.Bounds.Y + 6, e.Bounds.Width - 20, 20);
        TextRenderer.DrawText(e.Graphics, entry.Term, Theme.FontBody, termRect, Theme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var categoryRect = new Rectangle(e.Bounds.X + 16, e.Bounds.Bottom - 20, e.Bounds.Width - 20, 16);
        TextRenderer.DrawText(e.Graphics, entry.Category, Theme.FontSmall, categoryRect, Theme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
    }

    private void ShowSelected()
    {
        if (_termList.SelectedIndex < 0)
            return;

        var entry = Entries[_termList.SelectedIndex];
        var contentWidth = _detailPanel.ClientSize.Width - _detailPanel.Padding.Horizontal;
        if (contentWidth <= 0)
            contentWidth = 600;

        // Built top-to-bottom, then added in reverse — Dock=Top stacking in
        // this codebase always puts the last-added control at the very top,
        // same rule used by every other screen's toolbar/header stack.
        var controlsInReadingOrder = new List<Control>
        {
            new LetterSpacedLabel
            {
                Text = entry.Term,
                Font = Theme.FontSubheader,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
            },
            new Label
            {
                Text = entry.Category.ToUpperInvariant(),
                Font = Theme.FontSmall,
                ForeColor = Theme.AccentPrimary,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                UseCompatibleTextRendering = true,
            },
            BuildWrappedLabel(entry.Summary, Theme.FontBodyBold, Theme.TextPrimary, contentWidth),
            new Panel { Dock = DockStyle.Top, Height = 12 },
            BuildWrappedLabel(entry.Explanation, Theme.FontBody, Theme.TextPrimary, contentWidth),
        };

        if (entry.BuildDiagram is not null)
        {
            controlsInReadingOrder.Add(new Panel { Dock = DockStyle.Top, Height = 20 });
            controlsInReadingOrder.Add(entry.BuildDiagram());
        }

        _detailPanel.SuspendLayout();
        _detailPanel.Controls.Clear();
        controlsInReadingOrder.Reverse();
        foreach (var control in controlsInReadingOrder)
            _detailPanel.Controls.Add(control);
        _detailPanel.ResumeLayout();
        _detailPanel.AutoScrollPosition = new Point(0, 0);
    }

    private static Label BuildWrappedLabel(string text, Font font, Color color, int width)
    {
        var measuredHeight = TextRenderer.MeasureText(text, font, new Size(Math.Max(width, 50), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
        return new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = measuredHeight + 6,
            UseMnemonic = false,
        };
    }
}
