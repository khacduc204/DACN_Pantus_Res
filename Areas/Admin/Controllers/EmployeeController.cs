using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    [PermissionAuthorize(PermissionKeys.EmployeeManagement)]
    public class EmployeeController : Controller
    {
        private static readonly HashSet<string> ManagedRoleNames = new(StringComparer.OrdinalIgnoreCase)
        {
            RoleNames.Admin,
            RoleNames.Manager,
            RoleNames.Staff
        };

        private static readonly Dictionary<string, int> RoleOrdering = new(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Admin] = 1,
            [RoleNames.Manager] = 2,
            [RoleNames.Staff] = 3
        };

        private readonly KDContext _context;
        private readonly IPasswordHasher<tblUser> _passwordHasher;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(KDContext context, IPasswordHasher<tblUser> passwordHasher, ILogger<EmployeeController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string role = "all", string status = "all")
        {
            var employeesData = await _context.tblUser
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Role != null && ManagedRoleNames.Contains(u.Role.RoleName))
                .ToListAsync();

            var normalizedRoleFilter = NormalizeFilter(role);
            var normalizedStatusFilter = NormalizeFilter(status);

            var filtered = employeesData.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(u =>
                    (!string.IsNullOrWhiteSpace(u.UserName) && u.UserName.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(u.FirstName) && u.FirstName.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(u.LastName) && u.LastName.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(u.PhoneNumber) && u.PhoneNumber.Contains(keyword)));
            }

            filtered = normalizedRoleFilter switch
            {
                "admin" => filtered.Where(u => string.Equals(u.Role?.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)),
                "manager" => filtered.Where(u => string.Equals(u.Role?.RoleName, RoleNames.Manager, StringComparison.OrdinalIgnoreCase)),
                "staff" => filtered.Where(u => string.Equals(u.Role?.RoleName, RoleNames.Staff, StringComparison.OrdinalIgnoreCase)),
                _ => filtered
            };

            filtered = normalizedStatusFilter switch
            {
                "active" => filtered.Where(u => u.IsActive),
                "inactive" => filtered.Where(u => !u.IsActive),
                _ => filtered
            };

            var roleOptions = await LoadRoleOptionsAsync();

            var viewModel = new EmployeeManagementViewModel
            {
                Employees = filtered
                    .OrderByDescending(u => u.IsActive)
                    .ThenBy(u => GetRoleOrder(u.Role?.RoleName))
                    .ThenBy(u => u.UserName)
                    .Select(u => new EmployeeListItemViewModel
                    {
                        Id = u.IdUser,
                        DisplayName = ComposeDisplayName(u),
                        UserName = u.UserName,
                        RoleName = u.Role?.RoleName ?? string.Empty,
                        RoleBadgeClass = ResolveRoleBadge(u.Role?.RoleName),
                        PhoneNumber = u.PhoneNumber,
                        LastLogin = u.LastLogin,
                        IsActive = u.IsActive,
                        AvatarUrl = u.Avatar,
                        Description = u.Description
                    })
                    .ToList(),
                SearchTerm = search,
                RoleFilter = normalizedRoleFilter,
                StatusFilter = normalizedStatusFilter,
                TotalEmployees = employeesData.Count,
                ActiveEmployees = employeesData.Count(u => u.IsActive),
                AdminCount = employeesData.Count(u => string.Equals(u.Role?.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)),
                ManagerCount = employeesData.Count(u => string.Equals(u.Role?.RoleName, RoleNames.Manager, StringComparison.OrdinalIgnoreCase)),
                StaffCount = employeesData.Count(u => string.Equals(u.Role?.RoleName, RoleNames.Staff, StringComparison.OrdinalIgnoreCase)),
                RoleOptions = roleOptions,
                SuccessMessage = TempData["Success"] as string
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var roles = await PopulateRoleOptionsAsync();
            var defaultRoleId = roles.FirstOrDefault(r => string.Equals(r.Name, RoleNames.Manager, StringComparison.OrdinalIgnoreCase))?.Id
                                ?? roles.FirstOrDefault()?.Id ?? 0;

            var model = new EmployeeFormModel
            {
                IsActive = true,
                RoleId = defaultRoleId
            };

            if (!roles.Any())
            {
                ModelState.AddModelError(string.Empty, "Chưa cấu hình vai trò nhân viên. Vui lòng tạo role trước.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeFormModel model)
        {
            await PopulateRoleOptionsAsync();

            var trimmedPassword = model.Password?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedPassword))
            {
                ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu cho nhân viên mới.");
            }
            else if (trimmedPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(model.Password), "Mật khẩu phải có tối thiểu 6 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedUserName = model.UserName.Trim();
            model.UserName = normalizedUserName;
            if (await _context.tblUser.AnyAsync(u => u.UserName == normalizedUserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            var role = await FindManagedRoleAsync(model.RoleId);
            if (role == null)
            {
                ModelState.AddModelError(nameof(model.RoleId), "Vai trò không hợp lệ.");
                return View(model);
            }

            var employee = new tblUser
            {
                UserName = normalizedUserName,
                FirstName = Normalize(model.FirstName),
                LastName = Normalize(model.LastName),
                PhoneNumber = Normalize(model.PhoneNumber),
                Description = Normalize(model.Description),
                IdRole = role.IdRole,
                IsActive = model.IsActive
            };
            employee.Password = _passwordHasher.HashPassword(employee, trimmedPassword!);

            _context.tblUser.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã tạo nhân viên mới thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await LoadEmployeeAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            await PopulateRoleOptionsAsync();

            var model = new EmployeeFormModel
            {
                Id = employee.IdUser,
                UserName = employee.UserName,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                PhoneNumber = employee.PhoneNumber,
                Description = employee.Description,
                RoleId = employee.IdRole,
                IsActive = employee.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeFormModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            await PopulateRoleOptionsAsync();

            var trimmedPassword = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password.Trim();
            if (trimmedPassword != null && trimmedPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(model.Password), "Mật khẩu phải có tối thiểu 6 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = await LoadEmployeeAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var normalizedUserName = model.UserName.Trim();
            model.UserName = normalizedUserName;
            var duplicateUser = await _context.tblUser
                .AnyAsync(u => u.IdUser != employee.IdUser && u.UserName == normalizedUserName);
            if (duplicateUser)
            {
                ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            var role = await FindManagedRoleAsync(model.RoleId);
            if (role == null)
            {
                ModelState.AddModelError(nameof(model.RoleId), "Vai trò không hợp lệ.");
                return View(model);
            }

            employee.UserName = normalizedUserName;
            employee.FirstName = Normalize(model.FirstName);
            employee.LastName = Normalize(model.LastName);
            employee.PhoneNumber = Normalize(model.PhoneNumber);
            employee.Description = Normalize(model.Description);
            employee.IdRole = role.IdRole;
            employee.IsActive = model.IsActive;

            if (trimmedPassword != null)
            {
                employee.Password = _passwordHasher.HashPassword(employee, trimmedPassword);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.tblUser
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdUser == id && u.Role != null && ManagedRoleNames.Contains(u.Role.RoleName));

            if (employee == null)
            {
                return NotFound();
            }

            var model = new EmployeeDetailsViewModel
            {
                Id = employee.IdUser,
                UserName = employee.UserName,
                FullName = ComposeDisplayName(employee),
                RoleName = employee.Role?.RoleName ?? string.Empty,
                RoleBadgeClass = ResolveRoleBadge(employee.Role?.RoleName),
                PhoneNumber = employee.PhoneNumber,
                Description = employee.Description,
                IsActive = employee.IsActive,
                LastLogin = employee.LastLogin,
                AvatarUrl = employee.Avatar
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var employee = await LoadEmployeeAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value == employee.IdUser && employee.IsActive)
            {
                TempData["Success"] = "Không thể tự khoá tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            employee.IsActive = !employee.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = employee.IsActive
                ? "Đã kích hoạt tài khoản nhân viên."
                : "Đã khoá tài khoản nhân viên.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectOption>> PopulateRoleOptionsAsync()
        {
            var roles = await LoadRoleOptionsAsync();
            ViewBag.RoleOptions = roles;
            return roles;
        }

        private async Task<List<SelectOption>> LoadRoleOptionsAsync()
        {
            var roles = await _context.tblRole
                .AsNoTracking()
                .Where(r => ManagedRoleNames.Contains(r.RoleName))
                .ToListAsync();

            return roles
                .OrderBy(r => GetRoleOrder(r.RoleName))
                .Select(r => new SelectOption
                {
                    Id = r.IdRole,
                    Name = r.RoleName
                })
                .ToList();
        }

        private async Task<tblRole?> FindManagedRoleAsync(int roleId)
        {
            return await _context.tblRole
                .FirstOrDefaultAsync(r => r.IdRole == roleId && ManagedRoleNames.Contains(r.RoleName));
        }

        private async Task<tblUser?> LoadEmployeeAsync(int id)
        {
            return await _context.tblUser
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdUser == id && u.Role != null && ManagedRoleNames.Contains(u.Role.RoleName));
        }

        private static string NormalizeFilter(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "all" : value.Trim().ToLowerInvariant();
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ComposeDisplayName(tblUser user)
        {
            var parts = new[] { user.LastName, user.FirstName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();
            return parts.Length > 0 ? string.Join(" ", parts) : user.UserName;
        }

        private static string ResolveRoleBadge(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return "bg-secondary-subtle text-secondary";
            }

            return roleName.ToLowerInvariant() switch
            {
                var r when r == RoleNames.Admin.ToLowerInvariant() => "bg-danger-subtle text-danger",
                var r when r == RoleNames.Manager.ToLowerInvariant() => "bg-primary-subtle text-primary",
                var r when r == RoleNames.Staff.ToLowerInvariant() => "bg-success-subtle text-success",
                _ => "bg-secondary-subtle text-secondary"
            };
        }

        private static int GetRoleOrder(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return int.MaxValue;
            }

            return RoleOrdering.TryGetValue(roleName, out var order) ? order : int.MaxValue - 1;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var id))
            {
                return id;
            }
            return null;
        }
    }
}
