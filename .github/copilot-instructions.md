# KD Restaurant AI Guide
## Architecture & Boot
- [Program.cs](Program.cs) wires up MVC, Razor runtime compilation, session, and cookie auth before seeding data via `DbSeeder.SeedAsync(app.Services)`; assume seeders have run each boot.
- The EF Core context in [Models/KDContext.cs](Models/KDContext.cs) maps every legacy `tbl*` table explicitly; extend the partial context instead of scattering entity-specific DbContext logic.
- Admin features live inside [Areas/Admin](Areas/Admin) with `[Authorize(Roles = RoleNames.AdminManagerStaff)]` plus `[PermissionAuthorize]`; public controllers under [Controllers](Controllers) must not rely on admin-only helpers.
- Shared UI fragments that hit the database are packaged as view components in [ViewComponents](ViewComponents) and rendered under `Views/Shared/Components/*` for reuse.
- Static assets and uploads reside in [wwwroot](wwwroot); avatar uploads expect `/uploads/avatars` to exist locally before testing.

## Data & Persistence
- Connection strings come from `DefaultConnection` in [appsettings.json](appsettings.json); override via user secrets instead of editing the checked-in file.
- [Data/DbSeeder.cs](Data/DbSeeder.cs) repairs legacy schemas with targeted T-SQL `ALTER` statements and seeds roles/statuses plus `admin/Admin@123`; follow the same pattern when backfilling nullable columns.
- Monetary fields such as `tblOrder.TotalAmount` and `tblOrder_detail.PriceSale` are integer VND amounts—keep totals in ints and avoid floating point arithmetic.
- Table metadata relies on cached helper lookups (`GetReservedTableStatusIdAsync`, etc.) in [Areas/Admin/Controllers/BookingsController.cs](Areas/Admin/Controllers/BookingsController.cs); add new statuses to `BookingStatusHelper` and refresh caches when introducing states.
- Schema changes flow through EF migrations in [Migrations](Migrations) (`dotnet ef migrations add <Name> --project KD_Restaurant.csproj` then `dotnet ef database update`).

## Auth, Session & Permissions
- Cookie auth in [Program.cs](Program.cs#L12-L40) sets custom login/logout paths; session middleware must stay before `UseAuthentication()`.
- [Controllers/AccountController.cs](Controllers/AccountController.cs) hashes new passwords yet upgrades legacy plaintext values; reuse that fallback when adjusting auth flows.
- Claims include `ClaimTypes.Role` and a custom `FullName`; manual sign-ins must populate both so admin routing and greetings work.
- Use [Extensions/SessionExtensions.cs](Extensions/SessionExtensions.cs) for JSON session payloads; `CartController` already depends on it for cart storage.
- Permission gating couples `[Authorize]` with `PermissionAuthorizeAttribute` in [Security](Security); always pass the matching `PermissionKeys` constant for new admin endpoints.

## Booking & Admin Ops
- [Areas/Admin/Controllers/BookingsController.cs](Areas/Admin/Controllers/BookingsController.cs) orchestrates daily boards by eager-loading customers, tables, and orders into `BookingManagementViewModel` plus status counters.
- Booking states derive text, badge classes, and dashboard tabs from [Utilities/BookingStatusHelper.cs](Utilities/BookingStatusHelper.cs); extend it whenever status IDs change.
- Admin tiles and history tabs expect `BookingManagementViewModel.TabCounters` and `ActiveTab`; keep those fields updated when reusing shared partials.
- Table assignment flips the previous table back to "available" before reserving the new one; follow `AssignTable` when adding similar workflows.
- Walk-in/check-in flows auto-create `tblOrder` rows and push table statuses via cached IDs; reuse `OrderSummaryHelper` when extending live order boards.

## Customer Ordering & Payments
- The public cart/checkout pipeline lives in [Controllers/CartController.cs](Controllers/CartController.cs): cart state stays in session, each submission creates a `tblBooking`, then `tblOrder` + `tblOrder_detail` rows.
- Line pricing always prefers `PriceSale` and stores it per detail row so later menu edits do not retroactively change history.
- MoMo payments run through [Services/MomoPaymentService.cs](Services/MomoPaymentService.cs) via the registered `IMomoPaymentService`; credentials live under the `MomoPayment` section of `appsettings`.
- `ConfirmPayment`, `MomoReturn`, and `MomoNotify` all converge on `MarkOrderPaidAsync`; hook into that path instead of duplicating settlement logic.
- Successful payments update `tblOrder.Status`, `PaymentMethod`, `PaymentTime`, and often booking status—key any automation off those columns.

## UI & Composition
- Razor pages pair strongly-typed models with `ViewBag` lookups (e.g., `ViewBag.Categories` in [Controllers/MenuController.cs](Controllers/MenuController.cs)) to avoid extra DTOs.
- Shared menus, sliders, and nav bars rely on `MenuTopViewComponent`, `MenuItemViewComponent`, and `SliderViewComponent`; build new reusable snippets the same way.
- Admin experiences expect data shaped like `CurrentOrderListViewModel`/`BookingManagementViewModel`; populate their tab counters and badges so shared partials render correctly.

## Build, Run & Ops
- Run `dotnet restore` once, then prefer `dotnet watch run --project KD_Restaurant.csproj` for hot reload; Razor runtime compilation is already enabled.
- The app auto-opens the configured `urls` (defaults to `http://localhost:5068`) using `Process.Start`; override via `ASPNETCORE_URLS` when hosting multiple instances.
- No automated tests exist—manually validate booking creation, cart checkout, and MoMo payment happy paths after changes.
- When schema drift occurs, run `dotnet ef database update` and restart so `DbSeeder` can reapply its safety checks.
