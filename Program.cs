using Microsoft.EntityFrameworkCore;
using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<KDContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddAuthentication("AdminCookieAuthentication")
    .AddCookie("AdminCookieAuthentication", options =>
    {
        options.LoginPath = "/Admin/Account/Login"; // Đường dẫn tới trang đăng nhập admin
        options.AccessDeniedPath = "/Admin/Account/AccessDenied"; // Đường dẫn khi bị từ chối truy cập
        // Có thể cấu hình thêm các options khác nếu cần
    });
// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = builder.Configuration["urls"] ?? "http://localhost:5068";

    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to launch browser: {ex.Message}");
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Thêm vào phần cấu hình services

app.Run();
