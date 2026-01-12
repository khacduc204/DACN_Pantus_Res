# KD Restaurant AI Guide
## Architecture & Boot
- [Program.cs](Program.cs) configures MVC + Razor runtime compilation, session, and cookie auth, then seeds via `DbSeeder.SeedAsync(app.Services)`; expect the database to be normalized each boot by the seeder.
- [Models/KDContext.cs](Models/KDContext.cs) is the single EF Core context mapping every legacy `tbl*` table; extend the partial context instead of creating ad-hoc contexts or raw SQL in controllers.
- Admin tooling lives under [Areas/Admin](Areas/Admin) and is gated by `[Authorize(Roles = RoleNames.AdminManagerStaff)]` + `[PermissionAuthorize]`; public controllers in [Controllers](Controllers) must avoid Admin-only helpers to keep unauthenticated flows clean.
- Shared DB-backed UI snippets are implemented as view components under [ViewComponents](ViewComponents) and rendered from `Views/Shared/Components/*` to keep layout razor pages skinny.
- File uploads (avatars, media) sit in [wwwroot](wwwroot); ensure `/uploads/avatars` exists before exercising profile updates.

## Data & Persistence
- Connection strings come from `DefaultConnection` in [appsettings.json](appsettings.json); override with user secrets rather than editing the committed file.
- [Data/DbSeeder.cs](Data/DbSeeder.cs) patches legacy schema gaps with T-SQL, seeds roles/permissions/statuses, and creates `admin/Admin@123`; replicate that pattern whenever adding safety migrations or default data.
- Monetary columns (e.g., `tblOrder.TotalAmount`, `tblOrder_detail.PriceSale`) are stored as integer VND; never switch to floating point math—always sum `int` values.
- Booking/table status IDs are cached inside [Areas/Admin/Controllers/BookingsController.cs](Areas/Admin/Controllers/BookingsController.cs); when adding a new status update both `BookingStatusHelper` and the cached lookup methods so tabs, badges, and counts stay synchronized.
- Use EF migrations in [Migrations](Migrations) (`dotnet ef migrations add <Name> --project KD_Restaurant.csproj` → `dotnet ef database update`); DbSeeder assumes schema drift is repaired before it runs.

## Auth, Session & Permissions
- Cookie auth is configured in [Program.cs](Program.cs#L12-L40) with custom login/logout paths; middleware order is fixed: `UseSession()` must precede `UseAuthentication()`.
- [Controllers/AccountController.cs](Controllers/AccountController.cs) hashes new passwords but can detect legacy plaintext values and rehash them; reuse this fallback when touching authentication flows.
- Claims always include `ClaimTypes.Role` plus a custom `FullName`; manual sign-ins must populate both to keep admin routing and greetings functional.
- Session payloads are JSON-serialized via [Extensions/SessionExtensions.cs](Extensions/SessionExtensions.cs); `CartController` relies on this to persist cart lines between requests.
- Admin permissions pair `[Authorize]` with `PermissionAuthorizeAttribute` from [Security](Security); always specify the correct `PermissionKeys` constant for any new Admin endpoints.

## Booking & Admin Operations
- The dashboard in [Areas/Admin/Controllers/BookingsController.cs](Areas/Admin/Controllers/BookingsController.cs) eagerly loads customers, tables, orders, and status metadata into `BookingManagementViewModel`, plus builds status counters used by shared partials.
- Status chips/tab filtering derive from [Utilities/BookingStatusHelper.cs](Utilities/BookingStatusHelper.cs); extend helper mappings when new IDs or labels appear.
- `AssignTable` releases the previous table (sets it back to “available”) before reserving the new one and updates branch linkage—follow that pattern whenever tables are reassigned.
- Check-in/check-out flows auto-create `tblOrder` rows, flip table statuses (`GetServingTableStatusIdAsync`), and adjust booking states; copy this logic when adding live ops features instead of reimplementing.
- Cancels and completions add `tblOrder_cancelled` records and free the table by setting `IdStatus` to the cached “available” ID—mirror that sequencing so dashboards stay accurate.

## Customer Ordering & Payments
- [Controllers/CartController.cs](Controllers/CartController.cs) is the public cart pipeline: carts live in session, checkout always creates a `tblBooking`, `tblOrder`, then detail rows before clearing the session.
- Line pricing uses `PriceSale ?? Price` and persists the actual amount on each `tblOrder_detail` so historical totals survive menu edits—do not recalc totals from catalog data later.
- MoMo integrations run through [Services/MomoPaymentService.cs](Services/MomoPaymentService.cs) registered as `IMomoPaymentService`; options load from the `MomoPayment` section in configuration.
- `ConfirmPayment`, `MomoReturn`, and `MomoNotify` all funnel into `MarkOrderPaidAsync`, which writes `PaymentMethod`, `PaymentTime`, and booking status; hook any new payment method into that helper instead of duplicating settlement updates.

## UI & Composition
- Razor views combine strongly typed models with `ViewBag` collections (see [Controllers/MenuController.cs](Controllers/MenuController.cs)) to avoid unnecessary DTOs.
- Reusable menus/sliders live in `MenuTopViewComponent`, `MenuItemViewComponent`, and `SliderViewComponent`; prefer new view components for any DB-bound partial to keep layout pages fast.
- Admin dashboards expect `BookingManagementViewModel`/`CurrentOrderListViewModel`-shaped data with tab counters, badge classes, and order lookups populated; missing fields break shared partials immediately.

## Build, Run & Ops
- Standard loop: `dotnet restore`, then `dotnet watch run --project KD_Restaurant.csproj` for hot reload (Razor runtime compilation already enabled).
- On boot, [Program.cs](Program.cs#L35-L56) auto-launches the configured `urls` (default `http://localhost:5068`); override with `ASPNETCORE_URLS` when running multiple instances.
- There are no automated tests—smoke test booking creation, admin check-in/out, and MoMo happy paths after changes.
- When schema drift or data corruption occurs, run `dotnet ef database update` and restart so `DbSeeder` can reapply its safety checks.
