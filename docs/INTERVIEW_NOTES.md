# Interview Notes

Quick-reference talking points. Not meant to be read top to bottom — skim before a conversation.

## The 30-second pitch

"I work with ERP software professionally — implementations, SQL Server, integrations, data migration — and wanted to build a manufacturing ERP from the development side. DartERP is a WinForms desktop app over Entity Framework Core and SQL Server, with an application/service layer that owns the business rules — vendor status checks, PO line requirements, unique serial numbers — so validation isn't scattered across UI event handlers. It covers customers, vendors, products/inventory, purchase orders, work orders, serialized inventory, quality control, ATF-style acquisition & disposition tracking, and real authentication with hashed passwords and user profiles, with a dashboard that's wired to live queries, not hardcoded numbers."

## What DartERP is

- Internal ERP for a fictional manufacturing company (firearms-industry flavor, zero engineering/technical data — products are pure business records: SKU, cost, price, quantity)
- Demo flow: Sign In → Dashboard (KPIs, attention-needed lists, two charts) → Customers → Vendors → Products/Inventory → Purchase Orders → Work Orders → Serialized Inventory → Quality Control → A&D Log → Database → Settings
- The A&D Log models a real regulatory concept (the ATF bound book) purely as a compliance/business record — who received each serialized item and how it left inventory — no different in kind from any other audit trail an ERP tracks
- Scoped deliberately: Sales Orders and a REST API were cut to keep the core loop polished, not left half-built; role-based *enforcement* was also cut — `Role` is a real column shown in the UI, but nothing gates a screen behind it yet

## Architecture

- Four projects: `Core` (models/enums/DTOs/interfaces, zero dependencies) → `Application` (services, validation) and `Infrastructure` (EF Core, repositories) both depend on `Core` only → `WinForms` depends on all three
- WinForms talks to data only through repository interfaces defined in `Core` — never touches `DartErpDbContext` directly
- Every module follows the same shape: `Controls/*ListControl` (grid + search, lives in the sidebar nav) + `Forms/*EditForm` (modal create/edit) + one `Application.Services.*Service`

## WinForms specifics worth mentioning

- Custom-drawn `NavButton` and `StatusBadge` controls (owner-drawn `Panel`/`Label` subclasses with `OnPaint` overrides) rather than default WinForms chrome, for a look closer to a real enterprise app
- The purchase order line grid is an **unbound** `DataGridView` — rows added/removed imperatively, not through `AllowUserToAddRows`, with `CellValueChanged`/`CellEndEdit` driving live total recalculation
- Real bug I hit and fixed: `ComboBox.SelectedValue`/`SelectedItem` silently no-op when set in a form's constructor, before the native handle exists — they fall back to the first bound item with no exception. Fixed by deferring the data-load to the form's `Load` event. Hit this in four different edit-existing-record dialogs before tracing it to the root cause; good example of a class of WinForms timing bug that's easy to miss because it fails silently rather than throwing.

## Authentication

- Passwords are hashed with PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`, built into .NET — no third-party crypto package), salted per-user, stored as a single self-describing string (`{iterations}.{salt}.{hash}`). Verification uses `CryptographicOperations.FixedTimeEquals` rather than a plain `==`, so a timing attack can't be used to guess the hash byte-by-byte.
- `PasswordHasher` lives in `DartERP.Core`, not `Application` — `Infrastructure`'s seeder needs it to hash the demo users' passwords at startup, and `Infrastructure` doesn't reference `Application`. `Core` is the only project both already depend on. Small example of a dependency-direction constraint driving where code has to live.
- The login screen's video background runs through WebView2 (`Microsoft.Web.WebView2.WinForms`), navigating a small local HTML file rather than raw GDI+ video decoding — the standard, well-supported way to get smooth autoplay/loop video in a WinForms app. Falls back to a static branded panel if the WebView2 Runtime genuinely isn't present, so a missing runtime degrades gracefully instead of crashing the app before anyone can sign in.
- `MainForm`/`LoginForm` are `Transient` in DI, not `Singleton` like everything else — logging out needs a fresh `MainForm` (new header identity) and a fresh `LoginForm` each time `Program.cs`'s sign-in loop comes back around. `CurrentUserContext` is the one `Singleton` addition, holding whoever's currently signed in for the process lifetime.

## Charts

- Two hand-drawn GDI+ charts on the Dashboard (`Controls/PieChart.cs`, `Controls/BarChart.cs`) rather than a charting library — the built-in `DataVisualization.Charting` looks dated, and something like LiveCharts2 means fighting a third-party theming API to hit the exact `#D4C6A6` brand tan. Two chart types was a small, contained amount of custom drawing given `StatusBadge`/`KpiCard` already do GDI+ card/pill rendering, and it gets pixel-exact color control for free.
- `PieChart` reuses `StatusColors.For(PurchaseOrderStatus)` — the same status→color mapping every grid's status column already uses — so the donut's colors mean the same thing everywhere else in the app, not a chart-only palette invented on the spot.
- Both charts and `DashboardListCard` share one `DashboardCard` base class (title bar + rounded card chrome) — extracted once three controls wanted the literal same `OnPaint` override, not planned in ahead of time.
- New aggregation queries backing the charts (`IPurchaseOrderRepository.GetCountsByStatusAsync`, `IProductRepository.GetInventoryValueByCategoryAsync`) are plain EF Core `GroupBy` + `ToDictionaryAsync`, following the same "repository owns the query, service just calls it" split as everything else in `DashboardService`.

## Database Explorer

- The one screen that deliberately breaks the app's own conventions: every other grid hand-curates its columns and goes through `Application.Services`; this one binds straight to a repository's raw `GetAllAsync()` and lets `DataGridView.AutoGenerateColumns` build columns via reflection, because the whole point is showing the actual schema, not another curated business view of it
- Navigation/collection properties (`Product.WorkOrders`, `PurchaseOrder.Vendor`, etc.) get hidden after binding by checking each auto-generated column's `ValueType` against a short list of scalar types — one generic rule, not per-entity column lists, so it stays "zero new code per table"
- `List<T>` doesn't support click-to-sort out of the box the way a `BindingList<T>` or `DataView` would; sorting is done by hand off `ColumnHeaderMouseClick` — reflect on the clicked column's `DataPropertyName`, sort the current rows, rebuild a concretely-typed `List<T>` (via `MakeGenericType`) so `AutoGenerateColumns` doesn't just show every column as "object", and rebind

## .NET / EF Core

- .NET 8, nullable reference types enabled solution-wide
- `IDbContextFactory<DartErpDbContext>` instead of an injected `DbContext` — a WinForms app has no per-request boundary the way ASP.NET Core does, so a single long-lived context would accumulate tracked entities and risk stale data across screens. Each repository method opens and disposes its own short-lived context.
- Fluent API configuration per entity (one file per entity under `Infrastructure/Configurations`) — unique indexes, `decimal(18,2)` precision, enum-to-string conversion for readability in SSMS, and deliberate `DeleteBehavior` choices (`Cascade` only for true parent/child pairs like PO→PO lines; `Restrict` everywhere else, since most "deletes" in this app are soft-deletes via `IsActive`)

## Repository pattern / DI

- Small `IRepository<T>` base (`GetByIdAsync`/`GetAllAsync`/`AddAsync`/`UpdateAsync`) plus entity-specific interfaces adding real query methods (`GetBelowReorderLevelAsync`, `GetActiveAsync`, number-generation helpers) — not a generic repository framework, since that would've added abstraction without saving real code here
- Everything registered `Singleton` in `Program.cs` — WinForms has no scope concept, and repositories are now stateless (they hold only the context factory), so `Singleton` is simpler than inventing an artificial scope

## LINQ

- Dashboard KPIs and attention-needed lists are LINQ queries translated to SQL by EF Core (`Where(p => p.QuantityOnHand <= p.ReorderLevel)`, `SumAsync(w => w.Quantity)`, etc.), not client-side loops
- `DashboardService.GetSummaryAsync` fires ten of these concurrently via `Task.WhenAll` — safe because each opens its own DbContext through the factory

## SQL Server

- LocalDB for development; connection string in `appsettings.json`, no secrets in source control
- Migrations applied automatically at app startup (`Database.Migrate()`) so there's no manual setup step for a fresh clone

## Business logic separation

- Every validation rule lives in an `Application.Services.*Service` method and throws `ValidationException` with a user-facing message — the UI layer catches it and displays `.Message` directly rather than re-deriving its own copy
- Examples: inactive vendors can't be used on new POs, a PO needs ≥1 line before it can leave Draft, a Completed/Cancelled work order is locked against edits, a serialized item can only be created against a work order for a serialized product

## Challenges actually encountered (not hypothetical)

1. **The ComboBox timing bug** above — the most interesting one, because it fails silently. Traced it by adding a temporary diagnostic `MessageBox` showing `_grid.Rows.Count`/`SelectedValue` state, which is a decent debugging story on its own.
2. **DataGridView `FormatException` on an unbound column** — setting `DefaultCellStyle.Format = "C2"` on a column with no `DataPropertyName` made `DataGridView`'s Fill-column width measurement pass try to format a null value before `CellFormatting` ever ran, throwing and silently truncating the grid to one row. Fixed by pre-formatting the string myself in `CellFormatting` instead of relying on the grid's format pipeline for unbound columns.
3. **FlowLayoutPanel overflow** — six KPI cards in a non-wrapping `FlowLayoutPanel` overflowed the window width, clipping the last card entirely with no visual warning. `WrapContents = true` fixed it; caught by actually taking a screenshot and looking, not by code review.
4. **Namespace collision** — naming the services project `DartERP.Application` collided with `System.Windows.Forms.Application` in `Program.cs`, since both live under the shared `DartERP` root namespace as far as C#'s unqualified-name lookup is concerned. Fully-qualified the one call site (`System.Windows.Forms.Application.Run`).
5. **Editable ComboBox auto-filling itself** — the Sign Up form's Role field (`ComboBoxStyle.DropDown` + `AutoCompleteMode.SuggestAppend`) showed the first suggestion pre-filled and highlighted the moment it was built, before any keystroke. Turned out that combination treats an empty Text as "matches everything" on first paint and appends the first list item as an auto-selected suggestion. Fixed by explicitly forcing `SelectedIndex = -1` and `Text = string.Empty` right after populating `Items`.
6. **Fixed-size dialog, variable-size content** — the Sign Up panel (7 fields) is taller than the Sign In panel (2 fields), and the login window's height was sized for Sign In. Confirm Password and the submit button were getting silently clipped off the bottom with no way to reach them — caught by actually filling out the form during verification, not by looking at the code. Fixed with `AutoScroll = true` on the containing panel, so it only kicks in when content actually overflows.

## Future improvements

- Sales Orders (mirrors Purchase Orders structurally)
- REST API over the Application service layer
- Role-based access *enforcement* — `Role` exists and displays today, but doesn't gate anything yet — plus audit trail on status transitions
- Barcode/label printing for serialized inventory
