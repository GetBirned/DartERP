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
            "The language and runtime the entire solution is built on.",
            "I built DartERP on .NET 8 with C# end to end — every project in the solution targets net8.0 (or net8.0-windows for the WinForms host), and nullable reference types are enabled solution-wide so the compiler catches null-reference bugs at build time instead of runtime. I leaned on newer C# features throughout — record types for DTOs, pattern matching in status checks, collection expressions for empty lists, and primary constructors where they cut down on ceremony — since a from-scratch project was a good excuse to actually use the modern syntax instead of defaulting to habits from older codebases."),
        new(
            "WinForms",
            "UI Framework",
            "The desktop UI framework — chosen deliberately, not by default.",
            "I picked WinForms over a web stack because I wanted this to read as a real internal line-of-business desktop app, which is still how a lot of ERP software actually ships in manufacturing. The tradeoff is that WinForms gives you almost nothing for free visually — no CSS, no built-in theming, no charting — so I built a custom styling layer on top of it (Theme.cs for a light/dark palette, ControlStyleExtensions for consistent button/grid/input styling, hand-drawn GDI+ controls for anything the stock toolbox couldn't do) rather than accepting the default gray-button look."),
        new(
            "Layered Architecture",
            "Architecture",
            "Four projects, one dependency direction, enforced by the compiler.",
            "I split the solution into DartERP.Core (models, enums, interfaces — zero dependencies), DartERP.Application (services and validation) and DartERP.Infrastructure (EF Core and repositories, both depending only on Core), and DartERP.WinForms (the UI, depending on all three). The point of a zero-dependency Core is that it forces the dependency arrows to only point one way — if a repository ever tried to reference a WinForms form, that's a compile error, not a code-review nit. WinForms never touches the DbContext directly either; it only talks through repository interfaces defined in Core, which is what actually keeps the UI swappable in theory, even though I never built a second UI to prove it.",
            () => new LayeredArchitectureDiagram()),
        new(
            "Dependency Injection",
            "Architecture",
            "One composition root, everything wired in Program.cs.",
            "Program.cs is the single place every service, repository, and form gets registered with Microsoft.Extensions.DependencyInjection — there's no service locator or manual `new`-ing scattered through the forms. Almost everything is registered Singleton, since WinForms has no per-request scope the way ASP.NET Core does and the repositories are stateless. The two exceptions are MainForm and LoginForm, which are Transient — logging out needs a genuinely fresh MainForm with a clean header identity and a fresh LoginForm each time the sign-in loop comes back around, so those two can't be long-lived singletons like everything else."),
        new(
            "Repository Pattern",
            "Architecture",
            "One interface per entity, IRepository<T> for the four methods every one of them shares.",
            "Every entity gets an interface like IPurchaseOrderRepository extending a small shared IRepository<T> (GetByIdAsync/GetAllAsync/AddAsync/UpdateAsync), plus whatever real query methods that entity actually needs. I didn't build a generic repository framework on top of that, on purpose — a fully generic Repository<T> would've added abstraction without saving real code, since almost every screen needs a query IRepository<T> alone can't express. Each repository method opens its own short-lived DbContext through an IDbContextFactory rather than holding one long-lived context, since a WinForms app has no natural request boundary to tie a context's lifetime to.",
            () => new DataFlowDiagram()),
        new(
            "Entity Framework Core",
            "Data Access",
            "Code-First, migrations, Fluent API configuration per entity.",
            "The schema is defined entirely in C# — one Configuration.cs file per entity under Infrastructure/Configurations implementing IEntityTypeConfiguration<T>, covering unique indexes, decimal(18,2) precision on money columns, and enum-to-string conversions so a raw SSMS query shows 'Submitted' instead of an integer. Every schema change goes through `dotnet ef migrations add`, and migrations apply automatically at app startup so there's no manual setup step after a fresh clone. I also spent real thought on delete behavior per relationship — cascade only for true parent/child pairs like a PO and its lines, restrict everywhere else, since most 'deletes' in this app are actually soft-deletes and an accidental cascade should be impossible by construction, not just by convention."),
        new(
            "SQL Server (LocalDB)",
            "Data Access",
            "A real relational database, running locally — no cloud dependency to demo the app.",
            "I used SQL Server LocalDB for development specifically so the whole thing runs on a fresh clone with zero setup — no Docker, no cloud connection string, no 'ask me for credentials' step. The connection string lives in appsettings.json with nothing sensitive in it, and a small IDesignTimeDbContextFactory implementation lets the `dotnet ef` CLI construct the context at design time without spinning up the whole DI container just to generate a migration."),
        new(
            "LINQ",
            "Data Access",
            "Queries expressed as C#, translated to SQL by EF Core.",
            "The dashboard's KPIs and attention-needed lists are LINQ queries EF Core translates to real SQL server-side — Where(p => p.QuantityOnHand <= p.ReorderLevel) for low-stock products, SumAsync for units in production — not loops filtering an already-loaded list in memory. The two chart aggregations (purchase orders by status, inventory value by category) are GroupBy + ToDictionaryAsync in the repositories, the natural LINQ shape for 'count/sum grouped by a column' rather than something I'd have hand-written in raw SQL."),
        new(
            "Async / Await",
            "Language & Runtime",
            "Every data-access call is async, all the way up to the UI event handler.",
            "Every repository and service method is async Task, and every button click handler and form Load event that touches data is an async lambda, so the UI thread never blocks on a database round-trip. DashboardService.GetSummaryAsync fires around a dozen of these queries concurrently via Task.WhenAll, which is safe specifically because each one opens its own DbContext through the factory — if they shared one context, running them concurrently would throw, since a DbContext isn't thread-safe."),
        new(
            "PBKDF2 Password Hashing",
            "Security",
            "Passwords are hashed, not encrypted — and the difference matters.",
            "I used Rfc2898DeriveBytes.Pbkdf2, built into .NET's System.Security.Cryptography (no third-party crypto package), with 100,000 iterations, a random 16-byte salt per user, and SHA-256. Hashing is deliberately one-way — there's no key that turns a hash back into the original password, unlike encryption, which is reversible and the wrong tool for this job even though it sounds similar. Verification uses CryptographicOperations.FixedTimeEquals rather than a plain comparison, so a timing attack can't be used to guess the stored hash one byte at a time — a small thing that costs nothing to do correctly."),
        new(
            "WebView2",
            "UI Framework",
            "A real browser engine, embedded, just for the login screen's video.",
            "The sign-in screen has a looping video background, and WinForms has no good native way to decode and loop video smoothly, so I embedded a WebView2 control navigating a small local HTML file instead of fighting raw GDI+ video decoding. It falls back to a static branded panel if the WebView2 Runtime genuinely isn't installed, so a missing runtime degrades gracefully instead of crashing the app before anyone can sign in."),
        new(
            "GDI+ Custom Drawing",
            "UI Framework",
            "No charting library, no icon library — every visual is hand-drawn.",
            "The dashboard's donut and bar charts, every sidebar icon, rounded card corners, status badges, and even the two diagrams on this exact screen are all plain GDI+ — Graphics.FillPath/DrawPath/DrawString in OnPaint overrides, not a NuGet charting package. I made that call because the built-in charting control looks dated and a third-party library means fighting its own theming API to hit this app's exact brand tan, whereas hand-drawing gives pixel-exact color control for free — and it's genuinely not that much code once a pattern like the shared rounded-card chrome is built once and reused."),
        new(
            "xUnit & Fake Repositories",
            "Testing",
            "44 tests, in-memory fakes, no real database in the loop.",
            "The test suite runs against hand-written in-memory Fake*Repository classes implementing the same interfaces the real EF Core repositories do, rather than hitting LocalDB — which is what keeps the whole suite running in about a tenth of a second and makes it safe to run constantly instead of only occasionally. The tests target business rules specifically: purchase order validation, unique serial number generation, work order date ordering, password hashing round-trips, status-history logging firing on real transitions and staying silent on no-op saves — logic that's actually worth protecting, not incidental plumbing."),
        new(
            "Git & GitHub",
            "Tooling",
            "Version control as the paper trail, and the repo itself as the deliverable.",
            "Every feature in this app went in as its own commit with a message explaining the why, not just the what, and the whole history is public on GitHub — so the commit log is part of what I'd want someone to look at, not just the final diff. I used feature-sized commits deliberately rather than one giant initial commit, since a reviewable history was part of the point of building this as a portfolio piece in the first place."),
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
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
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
