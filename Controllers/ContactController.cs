using System;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KD_Restaurant.Controllers
{
    public class ContactController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<ContactController> _logger;

        public ContactController(KDContext context, ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var contact = new tblContact
                {
                    Name = model.Name.Trim(),
                    Phone = model.Phone.Trim(),
                    Email = model.Email.Trim(),
                    Message = model.Message.Trim(),
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = model.Name.Trim()
                };

                _context.tblContact.Add(contact);
                await _context.SaveChangesAsync();

                TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi liên hệ");
                TempData["ContactError"] = "Không thể gửi liên hệ lúc này. Vui lòng thử lại sau.";
                return View(model);
            }
        }
    }
}
