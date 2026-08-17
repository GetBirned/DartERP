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
- The purchase order line grid is a Syncfusion `SfDataGrid` bound to a `BindingList<PurchaseOrderLineRow>` — the row type implements `INotifyPropertyChanged`, so editing Quantity or Unit Cost through the grid recomputes Line Total and the footer total automatically instead of needing a `CellValueChanged` handler to drive it by hand
- Real bug I hit and fixed: `ComboBox.SelectedValue`/`SelectedItem` silently no-op when set in a form's constructor, before the native handle exists — they fall back to the first bound item with no exception. Fixed by deferring the data-load to the form's `Load` event. Hit this in four different edit-existing-record dialogs before tracing it to the root cause; good example of a class of WinForms timing bug that's easy to miss because it fails silently rather than throwing.

## Authentication

- Passwords are hashed with PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`, built into .NET — no third-party crypto package), salted per-user, stored as a single self-describing string (`{iterations}.{salt}.{hash}`). Verification uses `CryptographicOperations.FixedTimeEquals` rather than a plain `==`, so a timing attack can't be used to guess the hash byte-by-byte.
- `PasswordHasher` lives in `DartERP.Core`, not `Application` — `Infrastructure`'s seeder needs it to hash the demo users' passwords at startup, and `Infrastructure` doesn't reference `Application`. `Core` is the only project both already depend on. Small example of a dependency-direction constraint driving where code has to live.
- The login screen's video background runs through WebView2 (`Microsoft.Web.WebView2.WinForms`), navigating a small local HTML file rather than raw GDI+ video decoding — the standard, well-supported way to get smooth autoplay/loop video in a WinForms app. Falls back to a static branded panel if the WebView2 Runtime genuinely isn't present, so a missing runtime degrades gracefully instead of crashing the app before anyone can sign in.
- `MainForm`/`LoginForm` are `Transient` in DI, not `Singleton` like everything else — logging out needs a fresh `MainForm` (new header identity) and a fresh `LoginForm` each time `Program.cs`'s sign-in loop comes back around. `CurrentUserContext` is the one `Singleton` addition, holding whoever's currently signed in for the process lifetime.

## Charts

- Both Dashboard charts run on Syncfusion's `ChartControl` (`Controls/SfPieChartCard.cs`, `Controls/SfBarChartCard.cs`) rather than a hand-drawn GDI+ donut/bar pair — see the Syncfusion section below for why.
- The pie card colors each slice via `ChartSeries.Styles[i].Interior` using `StatusColors.For(PurchaseOrderStatus)` — the same status→color mapping every grid's status column already uses — so the donut's colors mean the same thing everywhere else in the app, not a chart-only palette invented on the spot.
- Both chart cards and `DashboardListCard` share one `DashboardCard` base class (title bar + rounded card chrome), with the `ChartControl` just docked into `DashboardCard`'s `Body` panel like any other content.
- New aggregation queries backing the charts (`IPurchaseOrderRepository.GetCountsByStatusAsync`, `IProductRepository.GetInventoryValueByCategoryAsync`) are plain EF Core `GroupBy` + `ToDictionaryAsync`, following the same "repository owns the query, service just calls it" split as everything else in `DashboardService`.

## Syncfusion

- Every grid and chart in the app runs on Syncfusion's WinForms suite (`Syncfusion.SfDataGrid.WinForms`, `Syncfusion.Chart.Windows`) — added specifically because my interview listing calls out "experience with tools such as Syncfusion (or similar)" as a plus, and I wanted real usage, not a token integration in one corner
- Styled by hand from `Theme.cs` (`StyleAsSfDataGrid` in `ControlStyleExtensions`) rather than one of Syncfusion's prebuilt visual themes — a prebuilt theme means another package and a color scheme that won't match this app's tan-and-black brand
- Real gotcha: `SfDataGrid`'s `Style.CellStyle.BackColor` alone doesn't paint anything — cells are actually colored through the `QueryCellStyle` event, and `CellStyleInfo` has a separate `Interior` brush that has to be set alongside `BackColor` for the fill to actually show. Same split exists on the chart side (`ChartStyleInfo.Interior`).
- Real gotcha: `CellButtonClickEventArgs.Record` and `CellComboBoxSelectionChangedEventArgs.Record` are, despite the name, the grid's internal `DataRowBase` wrapper, not the bound object — the actual row is one level down, on `.RowData`. A naive `e.Record is MyRowType` cast just silently fails on every click with no exception, which is what made this one worth tracking down instead of assuming the feature simply didn't work.
- Real gotcha: navigating away from a screen mid-query (e.g. clicking a different nav item before a search finishes) disposes the control while its `async Task RefreshAsync()` continuation is still pending. `DataGridView` tolerated a `DataSource` assignment after disposal; `SfDataGrid` throws `ArgumentNullException` from deep inside its own `RefreshViewAndContainer`. Fixed with an `IsDisposed` guard right after every `await` that's followed by a grid touch — an existing race that Syncfusion's stricter disposal behavior turned into a real, reproducible crash.
- License key lives in a gitignored `appsettings.local.json`, never in source control since this repo is public; registered once at startup in `Program.cs` via `Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense`, optional at runtime so a missing key falls back to an unlicensed-trial dialog instead of failing to start

## Database Explorer

- The one screen that deliberately breaks the app's own conventions: every other grid hand-curates its columns and goes through `Application.Services`; this one binds straight to a repository's raw `GetAllAsync()` and lets `SfDataGrid.AutoGenerateColumns` build columns via reflection, because the whole point is showing the actual schema, not another curated business view of it
- Navigation/collection properties (`Product.WorkOrders`, `PurchaseOrder.Vendor`, etc.) get hidden in the `AutoGeneratingColumn` event by checking each column's underlying CLR property type against a short list of scalar types — one generic rule, not per-entity column lists, so it stays "zero new code per table". Real gotcha: `AutoGeneratingColumnArgs.Column.ColumnMemberType` is still `null` at the point this event fires, so the type has to come from reflecting the bound list's own generic argument instead (`_currentRows.GetType().GetGenericArguments()[0]`), not from the column itself.
- Sorting is native (`SfDataGrid.AllowSorting`) — the hand-rolled reflection-based sort this screen used to need with a plain `DataGridView`-bound `List<T>` (rebuild a concretely-typed list via `MakeGenericType` on every column-header click) is gone entirely now that the grid handles it.

## Audit trail on status transitions

- Two dedicated tables (`PurchaseOrderStatusHistory`, `WorkOrderStatusHistory`) instead of one polymorphic `EntityType`/`EntityId` audit table — same "no generic abstraction beyond `IRepository<T>`" philosophy as the rest of the schema, and a polymorphic `EntityId` couldn't carry a real FK constraint anyway
- What counts as a loggable event is a business-layer decision: `PurchaseOrderService`/`WorkOrderService` decide when to write a row (initial entry on create, another only when a save actually changes `Status` — not on every header edit), while the repository methods (`AddStatusHistoryAsync`) just insert whatever they're handed
- First `Application.Services` classes to take a `CurrentUserContext` dependency (previously only WinForms forms touched it) — zero DI risk since it's already a `Singleton` in the same container
- UI reuses `DashboardListCard` rather than a new control, with a `stacked` display mode added so its two lines of text (a status transition, then who/when) both get full width instead of the dashboard's default side-by-side name/dollar-amount split, which was too cramped for the PO/Work Order dialogs' narrower side panel

## Purchase Order attachments

- File I/O (copy on add, delete on remove) lives in the WinForms layer (`Local/PurchaseOrderAttachmentStore.cs`), not in `Application.Services` — same place profile-picture upload already lives, and for the same reason: it keeps `PurchaseOrderService.AddAttachmentAsync`/`RemoveAttachmentAsync` pure metadata persistence, testable against `FakePurchaseOrderRepository` with zero real disk access, exactly like the rest of this test suite
- `PurchaseOrderAttachmentsPanel` extends `DashboardCard` directly rather than `DashboardListCard` — the shared list-row control has no room for a per-row "Remove" action or an "+ Add" button, and bolting one on would've risked the dashboard's four other list cards. `DashboardCard` was already proven to support a fully custom `Body` by `PieChart`/`BarChart`, so this is the third thing to extend it that way, not a new pattern
- Multiple attachments per PO meant the storage convention couldn't just copy `ProfilePictureStore` (one file, keyed by `userId`) — each file gets a GUID name under a per-PO subfolder instead

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
