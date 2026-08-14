# Architecture

## Layers

```
DartERP.WinForms          Forms, Controls, styling helpers, DI composition root (Program.cs)
DartERP.Application        Services (business rules, validation), Validation/ValidationException
DartERP.Infrastructure     EF Core DbContext, Fluent API configurations, repositories, seed data
DartERP.Core                Models, Enums, DTOs, repository interfaces — no project references
```

`Core` has zero dependencies; `Application` and `Infrastructure` each depend only on `Core`, not on each other. `WinForms` depends on all three, but talks to `Infrastructure` only through the interfaces defined in `Core` (`ICustomerRepository`, `IPurchaseOrderRepository`, etc.) — never against `DartErpDbContext` directly. Every module in the UI follows the same shape: a `Controls/*ListControl` (search/filter + grid, wired into the sidebar nav) and a `Forms/*EditForm` (modal create/edit dialog), backed by one `Application.Services.*Service`.

## Dependency injection

`Program.cs` builds a single root `ServiceCollection` at startup:

- `AddDbContextFactory<DartErpDbContext>` — see below for why a factory instead of a plain `AddDbContext`
- Repositories and services registered as `Singleton`. WinForms has no per-request scope the way ASP.NET Core does, and every repository is now cheap and stateless (it holds only the `IDbContextFactory`, no state of its own — see below), so `Singleton` is simpler than manufacturing an artificial "scope" concept for a desktop app.
- `MainForm` itself is resolved from the container so its constructor can take `IServiceProvider` and resolve each module's service on demand as the user navigates.
- `MainForm` and `LoginForm` specifically are `Transient`, not `Singleton` — logging out needs a brand-new `MainForm` next time (fresh header identity for whoever signs in next) and a fresh `LoginForm` on every pass through `Program.cs`'s sign-in loop. Everything they depend on stays `Singleton` as usual; only the forms themselves get re-created.

## Authentication

`Program.cs`'s `Main()` wraps the whole app in a loop: show `LoginForm` (`ShowDialog()`), and only if it returns `DialogResult.OK` does it resolve and run a `MainForm`. `MainForm.LoggedOut` (set right before `Close()` on the account menu's Log Out) tells the loop whether to go around again (back to a fresh `LoginForm`) or exit for good — a plain window close leaves `LoggedOut` false, so that path behaves exactly like the app did before auth existed.

`CurrentUserContext` (`Application/CurrentUserContext.cs`) is a `Singleton` holding whoever's currently signed in, mutated in place by `SignIn`/`SignOut` — same reasoning as every other `Singleton` in this container: WinForms has no per-request scope to hang a "session" off of, so the simplest correct answer is a container-wide singleton that gets updated rather than replaced.

`PasswordHasher` (PBKDF2 via `Rfc2898DeriveBytes`, no third-party package) lives in `DartERP.Core`, not `Application`, even though hashing feels like business logic — `Infrastructure`'s `DbSeeder` needs to hash the seeded demo users' passwords at startup, and `Infrastructure` only references `Core`, not `Application`. `Core` is the one project both already depend on, so that's where anything both layers need has to live.

## Why `IDbContextFactory<T>` instead of an injected `DbContext`

A WinForms app is one long-lived process with one window that stays open for the whole session — there's no natural "this unit of work is done, throw away the context" boundary the way an HTTP request gives you one for free in ASP.NET Core. Holding a single injected `DbContext` for the app's entire lifetime means its change tracker keeps growing (every entity ever loaded stays tracked) and screens can end up looking at stale data loaded minutes ago.

Every repository here takes `IDbContextFactory<DartErpDbContext>` and opens a fresh, short-lived context per method call:

```csharp
public async Task<List<Customer>> GetAllAsync()
{
    await using var context = await _contextFactory.CreateDbContextAsync();
    return await context.Customers.OrderBy(c => c.CompanyName).ToListAsync();
}
```

This is the pattern Microsoft's own docs recommend for WPF/WinForms + EF Core, and it sidesteps an entire category of "why does this screen show old data" bugs.

## Business logic placement

Validation and business rules live in `Application.Services`, not in WinForms event handlers or in the DbContext. Every service throws `DartERP.Application.Validation.ValidationException` with a plain-language message on a rule violation; the UI layer catches it and shows the message directly (see `PurchaseOrderEditForm.SaveAsync`, for example) rather than re-deriving its own copy. Examples of rules that live here rather than in the UI:

- A purchase order can't reference an inactive vendor (`PurchaseOrderService.ValidateVendorAsync`)
- A purchase order needs at least one line before it can move past Draft (`PurchaseOrderService.ValidateLines`)
- A work order that's Completed or Cancelled can't be edited (`WorkOrderService.IsLocked`)
- A serialized item can only be created against a work order whose product is actually serialized (`SerializedItemService.CreateAsync`)

## Purchase order line editing

The purchase order line grid (`PurchaseOrderEditForm`) is an unbound `DataGridView` — rows are added/removed imperatively via a "+ Add Line" button and a per-row Remove button, rather than relying on `DataGridView.AllowUserToAddRows`. Product selection auto-fills a default unit cost from the catalog (still editable), and quantity/cost edits recompute that row's line total and the order grand total immediately via `CellValueChanged`/`CellEndEdit`.

## A ComboBox binding gotcha worth documenting

`ComboBox.SelectedValue` and `SelectedItem` (when the box is bound via `DataSource`) don't reliably take effect until the control's native window handle exists. Setting them directly in a form's constructor — before `ShowDialog()` — fails silently: no exception, it just falls back to the first bound item. This bit four edit-existing-record flows during development (Vendor type, Product category, Work Order product/status, PO vendor/status) before being traced to this cause. The fix used throughout this codebase is to defer the data-population call to the form's `Load` event instead of running it inline in the constructor — see `WorkOrderEditForm`, `PurchaseOrderEditForm`, `VendorEditForm`, and `ProductEditForm` for the pattern.

## Dashboard aggregation

`DashboardService.GetSummaryAsync` fires ten repository calls via `Task.WhenAll` rather than sequential `await`s. This is safe specifically because every repository call opens its own `DbContext` through the factory — there's no shared connection or tracked-entity state that concurrent calls could corrupt.
