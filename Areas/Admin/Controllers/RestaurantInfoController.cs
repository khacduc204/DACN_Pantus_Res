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
using Microsoft.Extensions.Logging;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminOrManager)]
    [PermissionAuthorize(PermissionKeys.RestaurantSettings)]
    public class RestaurantInfoController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<RestaurantInfoController> _logger;

        public RestaurantInfoController(KDContext context, ILogger<RestaurantInfoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var entity = await _context.tblRestaurantInfo
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var model = entity == null
                ? new RestaurantInfoViewModel
                {
                    ResName = "KD Restaurant",
                    SortDescription = "Giữ câu chuyện thương hiệu sống động ở phần giới thiệu",
                    LogDescription = "<p>Hãy chia sẻ câu chuyện, triết lý ẩm thực và cam kết trải nghiệm của nhà hàng tại đây.</p>",
                    OpeningDay = "Thứ 2 - Chủ nhật",
                    OpenTime = new TimeSpan(9, 0, 0),
                    CloseTime = new TimeSpan(22, 0, 0)
                }
                : new RestaurantInfoViewModel
                {
                    Id = entity.Id,
                    ResName = entity.ResName,
                    SortDescription = entity.SortDescription,
                    LogDescription = entity.LogDescription ?? string.Empty,
                    Hotline1 = entity.Hotline1,
                    Hotline2 = entity.Hotline2,
                    Email = entity.Email,
                    Logo = entity.Logo,
                    OpeningDay = entity.OpeningDay,
                    OpenTime = entity.OpenTime,
                    CloseTime = entity.CloseTime
                };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RestaurantInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var entity = model.Id.HasValue
                ? await _context.tblRestaurantInfo.FirstOrDefaultAsync(x => x.Id == model.Id.Value)
                : await _context.tblRestaurantInfo.FirstOrDefaultAsync();

            if (entity == null)
            {
                entity = new tblRestaurantInfo();
                _context.tblRestaurantInfo.Add(entity);
            }

            entity.ResName = model.ResName.Trim();
            entity.SortDescription = Normalize(model.SortDescription);
            entity.LogDescription = model.LogDescription;
            entity.Hotline1 = Normalize(model.Hotline1);
            entity.Hotline2 = Normalize(model.Hotline2);
            entity.Email = Normalize(model.Email);
            entity.Logo = Normalize(model.Logo);
            entity.OpeningDay = Normalize(model.OpeningDay);
            entity.OpenTime = model.OpenTime;
            entity.CloseTime = model.CloseTime;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã lưu thông tin giới thiệu nhà hàng.";
            return RedirectToAction(nameof(Index));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
