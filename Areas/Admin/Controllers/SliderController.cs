using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SliderController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<SliderController> _logger;

        public SliderController(ILogger<SliderController> logger, KDContext context)
        {
            _context = context;
            _logger = logger;
        }

        // Hiển thị danh sách slider
        public IActionResult Index(string search)
        {
            var sliders = _context.tblSlider.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                sliders = sliders.Where(s =>
                    (s.Title != null && s.Title.Contains(search)) ||
                    (s.Description != null && s.Description.Contains(search))
                );
            }

            sliders = sliders.OrderBy(s => s.DisplayOrder).ThenBy(s => s.IdSlider);

            return View(sliders.ToList());
        }

        // GET: Thêm slider
        public IActionResult Create()
        {
            return View();
        }

        // POST: Thêm slider
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(tblSlider slider, IFormFile imageFile)
        {
            // Xử lý trạng thái checkbox
            slider.IsActive = Request.Form.ContainsKey("IsActive");

            // Kiểm tra có file ảnh không
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img", "slider");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                slider.ImagePath = "/assets/img/slider/" + fileName;
            }
            else
            {
                ModelState.AddModelError("ImagePath", "Vui lòng chọn ảnh cho slider.");
                return View(slider);
            }

            // Xóa lỗi ImagePath do [Required] trong Model
            ModelState.Remove("ImagePath");

            if (ModelState.IsValid)
            {
                slider.CreatedDate = DateTime.Now;
                _context.tblSlider.Add(slider);
                _context.SaveChanges();

                TempData["Success"] = "Thêm slider thành công!";
                return RedirectToAction("Index");
            }

            return View(slider);
        }

        // GET: Sửa slider
        public IActionResult Edit(int id)
        {
            var slider = _context.tblSlider.Find(id);
            if (slider == null)
                return NotFound();

            return View(slider);
        }

        // POST: Sửa slider
                [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(tblSlider slider, IFormFile? imageFile, string? OldImage)
        {
            // Loại bỏ validation không cần thiết
            ModelState.Remove("ImagePath"); // Quan trọng: tránh lỗi Required
        
            slider.IsActive = Request.Form.ContainsKey("IsActive");
        
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning(error.ErrorMessage);
                }
                return View(slider);
            }
        
            var dbSlider = _context.tblSlider.Find(slider.IdSlider);
            if (dbSlider == null)
                return NotFound();
        
            dbSlider.Title = slider.Title;
            dbSlider.Description = slider.Description;
            dbSlider.DisplayOrder = slider.DisplayOrder;
            dbSlider.IsActive = slider.IsActive;
        
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img", "slider");
        
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
        
                var filePath = Path.Combine(folderPath, fileName);
        
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }
        
                dbSlider.ImagePath = "/assets/img/slider/" + fileName;
            }
            else
            {
                // Giữ nguyên ảnh cũ nếu không upload mới
                dbSlider.ImagePath = OldImage ?? dbSlider.ImagePath;
            }
        
            _context.Update(dbSlider);
            _context.SaveChanges();
        
            TempData["Success"] = "Sửa slider thành công!";
            return RedirectToAction("Index");
        }

        // Xoá slider
        public IActionResult Delete(int id)
        {
            var slider = _context.tblSlider.Find(id);
            if (slider != null)
            {
                _context.tblSlider.Remove(slider);
                _context.SaveChanges();

                TempData["Success"] = "Xoá slider thành công!";
            }
            return RedirectToAction("Index");
        }

        // Đổi trạng thái hoạt động
        public IActionResult IsActive(int id)
        {
            var slider = _context.tblSlider.Find(id);
            if (slider != null)
            {
                slider.IsActive = !slider.IsActive;
                _context.Update(slider);
                _context.SaveChanges();

                TempData["Success"] = "Cập nhật trạng thái thành công!";
            }
            return RedirectToAction("Index");
        }
    }
}
