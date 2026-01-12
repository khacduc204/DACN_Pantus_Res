using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    [PermissionAuthorize(PermissionKeys.RoleManagement)]
    public class RoleController : Controller
    {
        private static readonly HashSet<string> HiddenRoles = new(new[] { "Customer" }, System.StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LockedRoleNames = new(new[] { RoleNames.Admin, RoleNames.Manager, RoleNames.Staff }, System.StringComparer.OrdinalIgnoreCase);

        private readonly KDContext _context;

        public RoleController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.tblRole
                .AsNoTracking()
                .Where(r => !HiddenRoles.Contains(r.RoleName))
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var roleIds = roles.Select(r => r.IdRole).ToList();
            var permissions = await _context.tblRolePermission
                .Where(rp => roleIds.Contains(rp.IdRole))
                .ToListAsync();

            var viewModel = new RolePermissionManagementViewModel
            {
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string,
                Roles = roles.Select(role => new RolePermissionCardViewModel
                {
                    RoleId = role.IdRole,
                    RoleName = role.RoleName,
                    Description = role.Description,
                    Permissions = PermissionKeys.All.ToDictionary(key => key, key => permissions.Any(p => p.IdRole == role.IdRole && p.PermissionKey == key && p.IsAllowed))
                }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.tblRole
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdRole == id && !HiddenRoles.Contains(r.RoleName));
            if (role == null)
            {
                return NotFound();
            }

            var granted = await _context.tblRolePermission
                .Where(rp => rp.IdRole == role.IdRole && rp.IsAllowed)
                .Select(rp => rp.PermissionKey)
                .ToListAsync();

            var model = new RolePermissionFormModel
            {
                RoleId = role.IdRole,
                RoleName = role.RoleName,
                Description = role.Description,
                GrantedPermissions = granted,
                IsLockedName = LockedRoleNames.Contains(role.RoleName)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RolePermissionFormModel model)
        {
            var role = await _context.tblRole.FirstOrDefaultAsync(r => r.IdRole == model.RoleId && !HiddenRoles.Contains(r.RoleName));
            if (role == null)
            {
                return NotFound();
            }

            model.IsLockedName = LockedRoleNames.Contains(role.RoleName);

            if (model.IsLockedName && !string.Equals(model.RoleName, role.RoleName, System.StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.RoleName), "Không thể đổi tên vai trò hệ thống.");
            }

            if (!model.IsLockedName)
            {
                var normalizedName = model.RoleName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(normalizedName))
                {
                    ModelState.AddModelError(nameof(model.RoleName), "Vui lòng nhập tên vai trò.");
                }
                else if (await _context.tblRole.AnyAsync(r => r.IdRole != role.IdRole && r.RoleName == normalizedName))
                {
                    ModelState.AddModelError(nameof(model.RoleName), "Tên vai trò đã tồn tại.");
                }
                else
                {
                    role.RoleName = normalizedName;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            role.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            var grantedSet = new HashSet<string>(model.GrantedPermissions ?? Enumerable.Empty<string>());
            var existingPermissions = await _context.tblRolePermission
                .Where(rp => rp.IdRole == role.IdRole)
                .ToListAsync();

            foreach (var key in PermissionKeys.All)
            {
                var entry = existingPermissions.FirstOrDefault(p => p.PermissionKey == key);
                if (entry == null)
                {
                    entry = new tblRolePermission
                    {
                        IdRole = role.IdRole,
                        PermissionKey = key,
                        IsAllowed = grantedSet.Contains(key)
                    };
                    _context.tblRolePermission.Add(entry);
                }
                else
                {
                    entry.IsAllowed = grantedSet.Contains(key);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật quyền truy cập cho vai trò.";
            return RedirectToAction(nameof(Index));
        }
    }
}
