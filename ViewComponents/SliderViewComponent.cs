using Microsoft.AspNetCore.Mvc;
using KD_Restaurant.Models;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.ViewComponents
{
    public class SliderViewComponent : ViewComponent
    {
        private readonly KDContext _context;

        public SliderViewComponent(KDContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var sliders = await _context.tblSlider
                                        .Where(s => s.IsActive == true)
                                        .OrderBy(s => s.DisplayOrder)
                                        .ToListAsync();

            return View(sliders); // Mặc định tìm file Views/Shared/Components/Slider/Default.cshtml
        }
    }
}
