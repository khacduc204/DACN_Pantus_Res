using KD_Restaurant.Models;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using KD_Restaurant.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace KD_Restaurant.Controllers
{
    public class AccountController : Controller
    {
        private readonly KDContext _context;
        private readonly IPasswordHasher<tblUser> _passwordHasher;
        private readonly IWebHostEnvironment _environment;
        private readonly IMembershipService _membershipService;

        public AccountController(KDContext context, IPasswordHasher<tblUser> passwordHasher, IWebHostEnvironment environment, IMembershipService membershipService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _environment = environment;
            _membershipService = membershipService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.tblUser
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.IsActive);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác");
                return View(model);
            }

            PasswordVerificationResult passwordResult;
            try
            {
                passwordResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
            }
            catch (FormatException)
            {
                if (string.Equals(user.Password, model.Password, StringComparison.Ordinal))
                {
                    user.Password = _passwordHasher.HashPassword(user, model.Password);
                    passwordResult = PasswordVerificationResult.Success;
                }
                else
                {
                    passwordResult = PasswordVerificationResult.Failed;
                }
            }
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác");
                return View(model);
            }

            user.LastLogin = DateTime.Now;
            await SyncCustomerProfileAsync(user, updateLastLogin: true);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer"),
                new Claim("FullName", string.Join(' ', new [] {user.LastName, user.FirstName}.Where(s => !string.IsNullOrWhiteSpace(s))))
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    AllowRefresh = true
                });

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            var roleName = user.Role?.RoleName;
            var isBackOfficeUser = string.Equals(roleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(roleName, RoleNames.Manager, StringComparison.OrdinalIgnoreCase)
                || string.Equals(roleName, RoleNames.Staff, StringComparison.OrdinalIgnoreCase);

            if (isBackOfficeUser)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.tblUser.AnyAsync(u => u.UserName == model.UserName);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            var customerRole = await _context.tblRole.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null)
            {
                ModelState.AddModelError(string.Empty, "Chưa cấu hình role mặc định. Liên hệ quản trị viên");
                return View(model);
            }

            var user = new tblUser
            {
                UserName = model.UserName.Trim(),
                FirstName = model.FirstName?.Trim(),
                LastName = model.LastName?.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                IdRole = customerRole.IdRole,
                IsActive = true
            };
            user.Password = _passwordHasher.HashPassword(user, model.Password);

            _context.tblUser.Add(user);
            await _context.SaveChangesAsync();

            await SyncCustomerProfileAsync(user, updateLastLogin: false);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var model = await BuildProfileViewModelAsync(user);
            model.SuccessMessage = TempData["ProfileSuccess"] as string;
            model.PasswordSuccess = TempData["PasswordSuccess"] as string;
            model.PasswordError = TempData["PasswordError"] as string;
            model.MembershipMessage = TempData["MembershipMessage"] as string;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollMembership()
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var customer = await SyncCustomerProfileAsync(user, updateLastLogin: false);
            var card = await _membershipService.EnrollCustomerAsync(customer);
            TempData["MembershipMessage"] = $"Đã kích hoạt thẻ thành viên #{card.CardNumber}.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null || user.IdUser != model.IdUser)
            {
                return RedirectToAction(nameof(Login));
            }

            string? avatarExtension = null;
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                avatarExtension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowedExtensions.Contains(avatarExtension))
                {
                    ModelState.AddModelError(nameof(model.AvatarFile), "Chỉ hỗ trợ các định dạng JPG, JPEG, PNG hoặc WEBP");
                }
                else if (model.AvatarFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.AvatarFile), "Ảnh đại diện tối đa 2MB");
                }
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildProfileViewModelAsync(user);
                invalidModel.LastName = model.LastName;
                invalidModel.FirstName = model.FirstName;
                invalidModel.PhoneNumber = model.PhoneNumber;
                invalidModel.Description = model.Description;
                return View(invalidModel);
            }

            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Description = model.Description?.Trim();

            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsRoot))
                {
                    Directory.CreateDirectory(uploadsRoot);
                }

                var extension = avatarExtension ?? Path.GetExtension(model.AvatarFile.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(uploadsRoot, fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                if (!string.IsNullOrWhiteSpace(user.Avatar))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, user.Avatar.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                user.Avatar = $"/uploads/avatars/{fileName}";
            }

            await SyncCustomerProfileAsync(user, updateLastLogin: false);
            await _context.SaveChangesAsync();

            TempData["ProfileSuccess"] = "Cập nhật thông tin cá nhân thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Vui lòng nhập mật khẩu hiện tại");
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải từ 6 ký tự trở lên");
            }

            if (!string.Equals(model.NewPassword, model.ConfirmPassword))
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Xác nhận mật khẩu không khớp");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildProfileViewModelAsync(user);
                invalidModel.PasswordError = "Không thể đổi mật khẩu. Vui lòng kiểm tra lại.";
                return View("Profile", invalidModel);
            }

            PasswordVerificationResult verify;
            try
            {
                verify = _passwordHasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword ?? string.Empty);
            }
            catch (FormatException)
            {
                // Legacy accounts may still have plain-text passwords; normalize them on first change.
                if (!string.Equals(user.Password, model.CurrentPassword, StringComparison.Ordinal))
                {
                    verify = PasswordVerificationResult.Failed;
                }
                else
                {
                    user.Password = _passwordHasher.HashPassword(user, model.CurrentPassword ?? string.Empty);
                    verify = PasswordVerificationResult.Success;
                }
            }
            if (verify == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng");
                var invalidModel = await BuildProfileViewModelAsync(user);
                invalidModel.PasswordError = "Không thể đổi mật khẩu. Vui lòng kiểm tra lại.";
                return View("Profile", invalidModel);
            }

            user.Password = _passwordHasher.HashPassword(user, model.NewPassword!);
            await _context.SaveChangesAsync();

            TempData["PasswordSuccess"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<tblUser?> LoadCurrentUserAsync()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idClaim))
            {
                return null;
            }

            if (!int.TryParse(idClaim, out var userId))
            {
                return null;
            }

            return await _context.tblUser.FirstOrDefaultAsync(u => u.IdUser == userId && u.IsActive);
        }

        private async Task<tblCustomer> SyncCustomerProfileAsync(tblUser user, bool updateLastLogin)
        {
            var customer = await _context.tblCustomer.FirstOrDefaultAsync(c => c.IdUser == user.IdUser);
            if (customer == null)
            {
                customer = new tblCustomer
                {
                    IdUser = user.IdUser,
                    IsActive = user.IsActive
                };
                _context.tblCustomer.Add(customer);
            }

            var fullName = ComposeFullName(user);
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                customer.FullName = fullName;
            }

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                customer.PhoneNumber = user.PhoneNumber;
            }

            customer.IsActive = user.IsActive;
            if (updateLastLogin)
            {
                customer.LastLogin = DateTime.Now;
            }

            return customer;
        }

        private static string? ComposeFullName(tblUser user)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(user.LastName))
            {
                parts.Add(user.LastName.Trim());
            }
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                parts.Add(user.FirstName.Trim());
            }

            return parts.Count > 0 ? string.Join(" ", parts).Trim() : null;
        }

        private async Task<ProfileViewModel> BuildProfileViewModelAsync(tblUser user)
        {
            var model = new ProfileViewModel
            {
                IdUser = user.IdUser,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Description = user.Description,
                AvatarUrl = user.Avatar
            };

            var customer = await _context.tblCustomer
                .Include(c => c.MembershipCard)
                .FirstOrDefaultAsync(c => c.IdUser == user.IdUser);

            if (customer?.MembershipCard != null)
            {
                var card = customer.MembershipCard;
                model.HasMembershipCard = true;
                model.MembershipCardNumber = card.CardNumber;
                model.MembershipPoints = card.Points;
                model.MembershipStatus = card.Status;
                model.MembershipCreatedDate = card.CreatedDate;

                var recentHistory = await _context.tblPointHistory
                    .Where(h => h.IdCard == card.IdCard)
                    .OrderByDescending(h => h.CreatedDate)
                    .Take(5)
                    .Select(h => new ProfileViewModel.PointHistoryItem
                    {
                        CreatedDate = h.CreatedDate,
                        ChangeType = h.ChangeType,
                        Points = h.Points,
                        ReferenceId = h.ReferenceId
                    })
                    .ToListAsync();

                model.MembershipHistory = recentHistory;
            }

            return model;
        }
    }
}
