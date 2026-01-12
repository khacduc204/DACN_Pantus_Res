using System;
using System.Collections.Generic;
using System.Linq;
using KD_Restaurant.Models;
using KD_Restaurant.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KD_Restaurant.Data
{
    public static class DbSeeder
    {
        private static readonly (string Name, string Description)[] DefaultRoles = new[]
        {
            (RoleNames.Admin, "Quản trị hệ thống"),
            (RoleNames.Manager, "Quản lý nhà hàng"),
            (RoleNames.Staff, "Nhân viên vận hành"),
            ("Customer", "Khách hàng")
        };

        private static readonly Dictionary<string, string[]> DefaultRolePermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Admin] = PermissionKeys.All.ToArray(),
            [RoleNames.Manager] = new[]
            {
                PermissionKeys.Dashboard,
                PermissionKeys.MenuStructure,
                PermissionKeys.MenuCatalog,
                PermissionKeys.MenuReviewManagement,
                PermissionKeys.BookingManagement,
                PermissionKeys.OrderManagement,
                PermissionKeys.TableManagement,
                PermissionKeys.SliderManagement,
                PermissionKeys.CustomerManagement,
                PermissionKeys.BranchManagement,
                PermissionKeys.RestaurantSettings
            },
            [RoleNames.Staff] = new[]
            {
                PermissionKeys.Dashboard,
                PermissionKeys.BookingManagement,
                PermissionKeys.OrderManagement,
                PermissionKeys.MenuReviewManagement,
                PermissionKeys.ContactManagement,
                PermissionKeys.TableManagement
            }
        };

        private static readonly (string Name, string Description)[] DefaultTableStatuses = new[]
        {
            ("Bàn trống", "Sẵn sàng tiếp khách"),
            ("Đang phục vụ", "Khách đang dùng bữa"),
            ("Đã đặt trước", "Có khách sẽ đến"),
            ("Bảo trì", "Tạm ngưng sử dụng")
        };

        /*private static readonly (string Name, int MaxSeats, string Description)[] DefaultTableTypes = new[]
        {
            ("Couple", 2, "Bàn 2 ghế cho cặp đôi"),
            ("Family", 4, "Bàn 4 ghế tiêu chuẩn"),
            ("Party", 8, "Bàn lớn dành cho tiệc nhỏ")
        };

        private static readonly (string TableName, int? AreaId, string TypeName, string StatusName, string Description)[] DefaultTables = new (string, int?, string, string, string)[]
        {
            ("Bàn A1", 1, "Couple", "Bàn trống", "Bàn cạnh cửa sổ"),
            ("Bàn A2", 1, "Couple", "Đã đặt trước", "Khách đặt lúc 19h"),
            ("Bàn B1", 2, "Family", "Đang phục vụ", "Phục vụ gia đình 4 người"),
            ("Bàn B2", 2, "Family", "Bàn trống", "Sẵn sàng đón khách"),
            ("Bàn C1", 3, "Party", "Bảo trì", "Đang kiểm tra hệ thống đèn"),
            ("Bàn C2", 3, "Party", "Đang phục vụ", "Tiệc sinh nhật 8 người"),
        };*/

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KDContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<tblUser>>();

            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(120));

            await EnsureSecurityTablesAsync(context);
            await EnsureTableManagementTablesAsync(context);

            var maxRoleId = await context.tblRole.AnyAsync()
                ? await context.tblRole.MaxAsync(r => r.IdRole)
                : 0;

            foreach (var role in DefaultRoles)
            {
                if (!await context.tblRole.AnyAsync(r => r.RoleName == role.Name))
                {
                    maxRoleId++;
                    context.tblRole.Add(new tblRole
                    {
                        IdRole = maxRoleId,
                        RoleName = role.Name,
                        Description = role.Description
                    });
                }
            }

            await context.SaveChangesAsync();

            await EnsureRolePermissionsAsync(context);

            await EnsureDefaultTableDataAsync(context);

            var adminRole = await context.tblRole.FirstAsync(r => r.RoleName == "Admin");

            if (!await context.tblUser.AnyAsync(u => u.UserName == "admin"))
            {
                var adminUser = new tblUser
                {
                    UserName = "admin",
                    FirstName = "Admin",
                    LastName = "KD",
                    IdRole = adminRole.IdRole,
                    IsActive = true
                };
                adminUser.Password = hasher.HashPassword(adminUser, "Admin@123");

                context.tblUser.Add(adminUser);
                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureSecurityTablesAsync(KDContext context)
        {
            const string ensureRoleTableScript = @"
IF OBJECT_ID(N'dbo.tblRole', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblRole](
        [IdRole] INT NOT NULL PRIMARY KEY,
        [RoleName] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(255) NULL
    );
END";

            const string ensureUserTableScript = @"
IF OBJECT_ID(N'dbo.tblUser', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblUser](
        [IdUser] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserName] NVARCHAR(50) NOT NULL,
        [Password] NVARCHAR(255) NOT NULL,
        [LastName] NVARCHAR(50) NULL,
        [FirstName] NVARCHAR(50) NULL,
        [Avatar] NVARCHAR(255) NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [IdRole] INT NOT NULL,
        [LastLogin] DATETIME NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Description] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblUser_UserName' AND object_id = OBJECT_ID('dbo.tblUser'))
BEGIN
    CREATE UNIQUE INDEX IX_tblUser_UserName ON [dbo].[tblUser]([UserName]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblUser_tblRole' AND parent_object_id = OBJECT_ID('dbo.tblUser'))
BEGIN
    ALTER TABLE [dbo].[tblUser] ADD CONSTRAINT [FK_tblUser_tblRole] FOREIGN KEY([IdRole]) REFERENCES [dbo].[tblRole]([IdRole]);
END;

IF COL_LENGTH('dbo.tblUser', 'Password') < 512
BEGIN
    ALTER TABLE [dbo].[tblUser] ALTER COLUMN [Password] NVARCHAR(512) NOT NULL;
END;

IF COL_LENGTH('dbo.tblUser', 'PhoneNumber') < 20
BEGIN
    ALTER TABLE [dbo].[tblUser] ALTER COLUMN [PhoneNumber] NVARCHAR(20) NULL;
END;

IF COL_LENGTH('dbo.tblUser', 'Avatar') IS NOT NULL AND COL_LENGTH('dbo.tblUser', 'Avatar') < 255
BEGIN
    ALTER TABLE [dbo].[tblUser] ALTER COLUMN [Avatar] NVARCHAR(255) NULL;
END";

            const string ensureRolePermissionTableScript = @"
IF OBJECT_ID(N'dbo.tblRolePermission', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblRolePermission](
        [IdRole] INT NOT NULL,
        [PermissionKey] NVARCHAR(100) NOT NULL,
        [IsAllowed] BIT NOT NULL DEFAULT 0,
        CONSTRAINT PK_tblRolePermission PRIMARY KEY CLUSTERED ([IdRole], [PermissionKey])
    );

    ALTER TABLE [dbo].[tblRolePermission]
    ADD CONSTRAINT FK_tblRolePermission_tblRole FOREIGN KEY([IdRole]) REFERENCES [dbo].[tblRole]([IdRole]) ON DELETE CASCADE;
END";

            await context.Database.ExecuteSqlRawAsync(ensureRoleTableScript);
            await context.Database.ExecuteSqlRawAsync(ensureUserTableScript);
            await context.Database.ExecuteSqlRawAsync(ensureRolePermissionTableScript);
        }

        private static async Task EnsureRolePermissionsAsync(KDContext context)
        {
            var roles = await context.tblRole.AsNoTracking().ToListAsync();
            var existing = await context.tblRolePermission.ToListAsync();

            foreach (var role in roles)
            {
                var defaultSet = DefaultRolePermissions.TryGetValue(role.RoleName, out var defaults)
                    ? new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var permission in PermissionKeys.All)
                {
                    var entry = existing.FirstOrDefault(e => e.IdRole == role.IdRole && e.PermissionKey == permission);
                    if (entry == null)
                    {
                        entry = new tblRolePermission
                        {
                            IdRole = role.IdRole,
                            PermissionKey = permission,
                            IsAllowed = defaultSet.Contains(permission)
                        };
                        context.tblRolePermission.Add(entry);
                        existing.Add(entry);
                    }
                    else
                    {
                        entry.IsAllowed = defaultSet.Contains(permission);
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureTableManagementTablesAsync(KDContext context)
        {
            const string ensureStatusTableScript = @"
IF OBJECT_ID(N'dbo.tblTable_status', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblTable_status](
        [IdStatus] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [StatusName] NVARCHAR(50) NULL,
        [isActive] BIT NOT NULL DEFAULT 1,
        [Description] NVARCHAR(255) NULL
    );
END";

            const string ensureTypeTableScript = @"
IF OBJECT_ID(N'dbo.tblTable_type', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblTable_type](
        [IdType] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TypeName] NVARCHAR(50) NULL,
        [MaxSeats] INT NULL,
        [isActive] BIT NOT NULL DEFAULT 1,
        [Description] NVARCHAR(255) NULL
    );
END";

            const string ensureAreaTableScript = @"
IF OBJECT_ID(N'dbo.tblArea', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblArea](
        [IdArea] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AreaName] NVARCHAR(100) NULL,
        [IdBranch] INT NULL,
        [isActive] BIT NOT NULL DEFAULT 1,
        [Description] NVARCHAR(255) NULL
    );
END";

            const string ensureTablesScript = @"
IF OBJECT_ID(N'dbo.tblTables', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tblTables](
        [IdTable] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TableName] NVARCHAR(50) NULL,
        [IdArea] INT NULL,
        [IdType] INT NULL,
        [IdStatus] INT NULL,
        [isActive] BIT NOT NULL DEFAULT 1,
        [Description] NVARCHAR(255) NULL
    );
END";

            const string ensureTableAreaNullableScript = @"
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'tblTables' AND c.name = 'IdArea' AND c.is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[tblTables] ALTER COLUMN [IdArea] INT NULL;
END";

            const string ensureTableTypeNullableScript = @"
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'tblTables' AND c.name = 'IdType' AND c.is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[tblTables] ALTER COLUMN [IdType] INT NULL;
END";

            const string ensureTableStatusNullableScript = @"
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'tblTables' AND c.name = 'IdStatus' AND c.is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[tblTables] ALTER COLUMN [IdStatus] INT NULL;
END";

            const string cleanupOrphanedTableReferencesScript = @"
IF EXISTS (
    SELECT 1 FROM [dbo].[tblTables] t
    LEFT JOIN [dbo].[tblTable_status] s ON t.IdStatus = s.IdStatus
    WHERE t.IdStatus IS NOT NULL AND s.IdStatus IS NULL)
BEGIN
    UPDATE t SET IdStatus = NULL
    FROM [dbo].[tblTables] t
    LEFT JOIN [dbo].[tblTable_status] s ON t.IdStatus = s.IdStatus
    WHERE t.IdStatus IS NOT NULL AND s.IdStatus IS NULL;
END;

IF EXISTS (
    SELECT 1 FROM [dbo].[tblTables] t
    LEFT JOIN [dbo].[tblTable_type] ty ON t.IdType = ty.IdType
    WHERE t.IdType IS NOT NULL AND ty.IdType IS NULL)
BEGIN
    UPDATE t SET IdType = NULL
    FROM [dbo].[tblTables] t
    LEFT JOIN [dbo].[tblTable_type] ty ON t.IdType = ty.IdType
    WHERE t.IdType IS NOT NULL AND ty.IdType IS NULL;
END;";

            const string ensureTableForeignKeysScript = @"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTables_tblTable_status_IdStatus')
BEGIN
    ALTER TABLE [dbo].[tblTables]
    ADD CONSTRAINT [FK_tblTables_tblTable_status_IdStatus]
    FOREIGN KEY([IdStatus]) REFERENCES [dbo].[tblTable_status]([IdStatus]) ON DELETE SET NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTables_tblTable_type_IdType')
BEGIN
    ALTER TABLE [dbo].[tblTables]
    ADD CONSTRAINT [FK_tblTables_tblTable_type_IdType]
    FOREIGN KEY([IdType]) REFERENCES [dbo].[tblTable_type]([IdType]) ON DELETE SET NULL;
END;";

            const string ensureAreaForeignKeysScript = @"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblArea_tblBranch_IdBranch')
BEGIN
    ALTER TABLE [dbo].[tblArea]
    ADD CONSTRAINT [FK_tblArea_tblBranch_IdBranch]
    FOREIGN KEY([IdBranch]) REFERENCES [dbo].[tblBranch]([IdBranch]) ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTables_tblArea_IdArea')
BEGIN
    ALTER TABLE [dbo].[tblTables]
    ADD CONSTRAINT [FK_tblTables_tblArea_IdArea]
    FOREIGN KEY([IdArea]) REFERENCES [dbo].[tblArea]([IdArea]) ON DELETE SET NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblTables_IdArea')
BEGIN
    CREATE INDEX IX_tblTables_IdArea ON [dbo].[tblTables]([IdArea]);
END";

            const string ensureBookingForeignKeyScript = @"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblBooking_tblTables_IdTable')
BEGIN
    ALTER TABLE [dbo].[tblBooking]
    ADD CONSTRAINT [FK_tblBooking_tblTables_IdTable]
    FOREIGN KEY([IdTable]) REFERENCES [dbo].[tblTables]([IdTable]) ON DELETE SET NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblBooking_IdTable')
BEGIN
    CREATE INDEX IX_tblBooking_IdTable ON [dbo].[tblBooking]([IdTable]);
END";

            await context.Database.ExecuteSqlRawAsync(ensureStatusTableScript);
            await context.Database.ExecuteSqlRawAsync(ensureTypeTableScript);
            await context.Database.ExecuteSqlRawAsync(ensureAreaTableScript);
            await context.Database.ExecuteSqlRawAsync(ensureTablesScript);
            await context.Database.ExecuteSqlRawAsync(ensureTableAreaNullableScript);
            await context.Database.ExecuteSqlRawAsync(ensureTableTypeNullableScript);
            await context.Database.ExecuteSqlRawAsync(ensureTableStatusNullableScript);
            await context.Database.ExecuteSqlRawAsync(cleanupOrphanedTableReferencesScript);
            await context.Database.ExecuteSqlRawAsync(ensureAreaForeignKeysScript);
            await context.Database.ExecuteSqlRawAsync(ensureTableForeignKeysScript);
            await context.Database.ExecuteSqlRawAsync(ensureBookingForeignKeyScript);
        }

        private static Task EnsureDefaultTableDataAsync(KDContext context)
        {
            // Các seed mẫu (trạng thái, loại bàn, danh sách bàn) đã bị vô hiệu hóa
            // để ứng dụng luôn sử dụng dữ liệu thực từ database hiện tại.
            return Task.CompletedTask;
        }
    }
}
