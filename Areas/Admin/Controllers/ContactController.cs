using System;
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
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.ContactManagement)]
    public class ContactController : Controller
    {
        private readonly KDContext _context;
        private const int PageSize = 15;

        public ContactController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string status = "unread", int page = 1)
        {
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? "unread"
                : status.Trim().ToLowerInvariant();

            var query = _context.tblContact.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(c =>
                    (c.Name != null && c.Name.Contains(keyword)) ||
                    (c.Email != null && c.Email.Contains(keyword)) ||
                    (c.Phone != null && c.Phone.Contains(keyword)) ||
                    (c.Message != null && c.Message.Contains(keyword)));
            }

            query = normalizedStatus switch
            {
                "read" => query.Where(c => c.IsRead),
                "all" => query,
                _ => query.Where(c => !c.IsRead)
            };

            page = Math.Max(1, page);
            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var contacts = await query
                .OrderBy(c => c.IsRead)
                .ThenByDescending(c => c.CreatedDate ?? DateTime.MinValue)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new ContactAdminListItemViewModel
                {
                    Id = c.IdContact,
                    Name = string.IsNullOrWhiteSpace(c.Name) ? "Khách lạ" : c.Name!,
                    Phone = c.Phone,
                    Email = c.Email,
                    Message = c.Message,
                    IsRead = c.IsRead,
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();

            var unreadCount = await _context.tblContact.CountAsync(c => !c.IsRead);
            var readCount = await _context.tblContact.CountAsync(c => c.IsRead);

            var viewModel = new ContactAdminIndexViewModel
            {
                Contacts = contacts,
                Search = search,
                StatusFilter = normalizedStatus,
                UnreadCount = unreadCount,
                ReadCount = readCount,
                TotalCount = unreadCount + readCount,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = PageSize
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRead(int id, string? status, string? search, int page = 1)
        {
            var contact = await _context.tblContact.FindAsync(id);
            if (contact == null)
            {
                TempData["Error"] = "Không tìm thấy liên hệ.";
                return RedirectToAction(nameof(Index), new { status, search, page });
            }

            contact.IsRead = !contact.IsRead;
            contact.ModifiedDate = DateTime.Now;
            contact.ModifiedBy = User.Identity?.Name;
            _context.tblContact.Update(contact);
            await _context.SaveChangesAsync();

            TempData["Success"] = contact.IsRead
                ? "Đã đánh dấu đã đọc."
                : "Đã chuyển liên hệ về trạng thái chưa đọc.";

            var redirectStatus = string.IsNullOrWhiteSpace(status)
                ? (contact.IsRead ? "read" : "unread")
                : status;

            return RedirectToAction(nameof(Index), new { status = redirectStatus, search, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? status, string? search, int page = 1)
        {
            var contact = await _context.tblContact.FindAsync(id);
            if (contact == null)
            {
                TempData["Error"] = "Không tìm thấy liên hệ.";
                return RedirectToAction(nameof(Index), new { status, search, page });
            }

            _context.tblContact.Remove(contact);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xoá liên hệ.";

            return RedirectToAction(nameof(Index), new { status, search, page });
        }
    }
}
