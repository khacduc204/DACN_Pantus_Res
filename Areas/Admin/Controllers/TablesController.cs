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
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.TableManagement)]
    public class TablesController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<TablesController> _logger;

        public TablesController(KDContext context, ILogger<TablesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? branchId = null)
        {
            var tables = await BuildTableCardsAsync();
            var statuses = await BuildStatusesAsync();
            var areas = await _context.tblArea
                .Include(a => a.Branch)
                .Where(a => a.isActive)
                .OrderBy(a => a.AreaName)
                .ToListAsync();
            var branches = await _context.tblBranch
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();
            var types = await _context.tblTable_type
                .Where(t => t.isActive)
                .OrderBy(t => t.TypeName)
                .ToListAsync();

            var viewModel = new TableManagementViewModel
            {
                Tables = tables,
                Statuses = statuses,
                Branches = BuildBranchGroups(areas, tables),
                CreateOptions = BuildCreateOptions(branches, areas, types, statuses),
                SelectedBranchId = branchId
            };

            viewModel.StatusCounters = tables
                .GroupBy(t => t.StatusId ?? -1)
                .ToDictionary(group => group.Key, group => group.Count());

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int tableId, int statusId)
        {
            var table = await _context.tblTables
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.IdTable == tableId);
            var status = await _context.tblTable_status.FirstOrDefaultAsync(s => s.IdStatus == statusId);

            if (table == null || status == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin bàn hoặc trạng thái." });
            }

            var normalizedName = (status.StatusName ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedName.Contains("đang phục vụ"))
            {
                var activeBooking = await _context.tblBooking
                    .Where(b => b.isActive && b.IdTable == table.IdTable)
                    .OrderByDescending(b => b.BookingDate)
                    .FirstOrDefaultAsync();
                if (activeBooking == null)
                {
                    return Json(new { success = false, message = "Không có đặt bàn nào đang phục vụ để gán trạng thái này." });
                }
            }

            if (normalizedName.Contains("đã đặt"))
            {
                var assignedBooking = await _context.tblBooking
                    .Where(b => b.isActive && b.IdTable == table.IdTable)
                    .OrderBy(b => b.BookingDate)
                    .FirstOrDefaultAsync();
                if (assignedBooking == null)
                {
                    return Json(new { success = false, message = "Bàn chưa có đặt trước nào, không thể chuyển trạng thái." });
                }
            }

            if (normalizedName.Contains("trống"))
            {
                var relatedBookings = await _context.tblBooking
                    .Where(b => b.IdTable == table.IdTable && b.isActive)
                    .ToListAsync();
                foreach (var booking in relatedBookings)
                {
                    booking.IdTable = null;
                }
            }

            table.IdStatus = statusId;
            table.isActive = true; // đảm bảo bàn luôn hoạt động khi thay đổi trạng thái
            _context.tblTables.Update(table);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật trạng thái cho bàn {TableId}", tableId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lưu thay đổi." });
            }

            return Json(new
            {
                success = true,
                statusName = status.StatusName ?? "Không xác định",
                badgeClass = ResolveBadgeClass(status.StatusName)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TableCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                return Json(new { success = false, message = errorMessage });
            }

            var area = await _context.tblArea.Include(a => a.Branch)
                .FirstOrDefaultAsync(a => a.IdArea == request.AreaId && a.isActive);
            if (area == null)
            {
                return Json(new { success = false, message = "Khu vực không hợp lệ hoặc đã bị khóa." });
            }

            if (request.TypeId.HasValue && !await _context.tblTable_type.AnyAsync(t => t.IdType == request.TypeId.Value))
            {
                return Json(new { success = false, message = "Loại bàn không tồn tại." });
            }

            if (request.StatusId.HasValue && !await _context.tblTable_status.AnyAsync(s => s.IdStatus == request.StatusId.Value))
            {
                return Json(new { success = false, message = "Trạng thái không tồn tại." });
            }

            var entity = new tblTable
            {
                TableName = request.TableName.Trim(),
                IdArea = request.AreaId,
                IdType = request.TypeId,
                IdStatus = request.StatusId,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                isActive = request.IsActive
            };

            _context.tblTables.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo bàn mới");
                return Json(new { success = false, message = "Không thể lưu bàn mới. Vui lòng thử lại." });
            }

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action(nameof(Index))
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TableEditRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                return Json(new { success = false, message = errorMessage });
            }

            var table = await _context.tblTables.FirstOrDefaultAsync(t => t.IdTable == request.Id);
            if (table == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bàn cần chỉnh sửa." });
            }

            var area = await _context.tblArea.Include(a => a.Branch)
                .FirstOrDefaultAsync(a => a.IdArea == request.AreaId && a.isActive);
            if (area == null)
            {
                return Json(new { success = false, message = "Khu vực không hợp lệ hoặc đã bị khóa." });
            }

            if (request.TypeId.HasValue && !await _context.tblTable_type.AnyAsync(t => t.IdType == request.TypeId.Value))
            {
                return Json(new { success = false, message = "Loại bàn không tồn tại." });
            }

            if (request.StatusId.HasValue && !await _context.tblTable_status.AnyAsync(s => s.IdStatus == request.StatusId.Value))
            {
                return Json(new { success = false, message = "Trạng thái không tồn tại." });
            }

            table.TableName = request.TableName.Trim();
            table.IdArea = request.AreaId;
            table.IdType = request.TypeId;
            table.IdStatus = request.StatusId;
            table.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            table.isActive = true; // luôn giữ bàn hoạt động khi chỉnh sửa

            _context.tblTables.Update(table);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật bàn {TableId}", request.Id);
                return Json(new { success = false, message = "Không thể lưu thay đổi của bàn. Vui lòng thử lại." });
            }

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action(nameof(Index), new { branchId = area.IdBranch })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateArea(AreaCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                return Json(new { success = false, message = errorMessage });
            }

            var branch = await _context.tblBranch.FirstOrDefaultAsync(b => b.IdBranch == request.BranchId && b.IsActive);
            if (branch == null)
            {
                return Json(new { success = false, message = "Cơ sở không tồn tại hoặc đã bị khóa." });
            }

            var area = new tblArea
            {
                AreaName = request.AreaName.Trim(),
                IdBranch = request.BranchId,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                isActive = request.IsActive
            };

            _context.tblArea.Add(area);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo khu vực mới");
                return Json(new { success = false, message = "Không thể lưu khu vực mới. Vui lòng thử lại." });
            }

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action(nameof(Index), new { branchId = request.BranchId })
            });
        }

        private async Task<List<TableCardViewModel>> BuildTableCardsAsync()
        {
            var tables = await _context.tblTables
                .Include(t => t.Status)
                .Include(t => t.Type)
                .Include(t => t.Area)
                    .ThenInclude(a => a!.Branch)
                .Where(t => t.isActive)
                .OrderBy(t => t.TableName)
                .ToListAsync();

            var today = DateTime.Today;
            var activeBookings = await _context.tblBooking
                .Where(b => b.isActive && b.IdTable != null && b.BookingDate.Date == today)
                .Include(b => b.Customer)
                .ToListAsync();

            var bookingLookup = activeBookings
                .Where(b => b.IdTable.HasValue)
                .GroupBy(b => b.IdTable!.Value)
                .ToDictionary(group => group.Key, group => group.OrderBy(b => b.BookingDate).First());

            var cards = new List<TableCardViewModel>();
            foreach (var table in tables)
            {
                bookingLookup.TryGetValue(table.IdTable, out var booking);

                cards.Add(new TableCardViewModel
                {
                    Id = table.IdTable,
                    TableName = string.IsNullOrWhiteSpace(table.TableName) ? $"Bàn {table.IdTable}" : table.TableName,
                    AreaId = table.IdArea,
                    AreaLabel = table.Area?.AreaName ?? "Chưa gán khu vực",
                    AreaDescription = table.Area?.Description,
                    BranchId = table.Area?.IdBranch,
                    BranchLabel = table.Area?.Branch?.BranchName,
                    TypeId = table.IdType,
                    TypeName = table.Type?.TypeName,
                    MaxSeats = table.Type?.MaxSeats,
                    StatusId = table.IdStatus,
                    StatusName = table.Status?.StatusName ?? "Không xác định",
                    StatusBadgeClass = ResolveBadgeClass(table.Status?.StatusName),
                    Description = table.Description,
                    HasActiveBooking = booking != null,
                    CustomerName = booking?.Customer?.FullName,
                    BookingTimeFrame = booking != null ? $"{booking.BookingDate:dd/MM} · {booking.TimeSlot}" : null,
                    BookingNote = booking?.Note,
                    IsActive = table.isActive
                });
            }

            return cards;
        }

        private async Task<List<TableStatusOption>> BuildStatusesAsync()
        {
            var statuses = await _context.tblTable_status
                .Where(s => s.isActive)
                .OrderBy(s => s.StatusName)
                .ToListAsync();

            return statuses.Select(status => new TableStatusOption
            {
                Id = status.IdStatus,
                Name = status.StatusName ?? "Không xác định",
                BadgeClass = ResolveBadgeClass(status.StatusName)
            }).ToList();
        }

        private List<BranchGroupViewModel> BuildBranchGroups(List<tblArea> areas, List<TableCardViewModel> tables)
        {
            var areaIdSet = new HashSet<int>(areas.Select(a => a.IdArea));
            var tablesByArea = tables
                .Where(t => t.AreaId.HasValue)
                .GroupBy(t => t.AreaId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.TableName).ToList());

            var groups = areas
                .GroupBy(a => new
                {
                    a.IdBranch,
                    BranchName = string.IsNullOrWhiteSpace(a.Branch?.BranchName)
                        ? $"Chi nhánh #{a.IdBranch}"
                        : a.Branch!.BranchName
                })
                .OrderBy(g => g.Key.BranchName)
                .Select(g => new BranchGroupViewModel
                {
                    BranchId = g.Key.IdBranch,
                    BranchName = g.Key.BranchName ?? "Chi nhánh chưa đặt tên",
                    Areas = g.OrderBy(a => a.AreaName ?? $"Khu #{a.IdArea}")
                        .Select(a => new AreaGroupViewModel
                        {
                            AreaId = a.IdArea,
                            AreaName = string.IsNullOrWhiteSpace(a.AreaName) ? $"Khu #{a.IdArea}" : a.AreaName!,
                            AreaDescription = a.Description,
                            Tables = tablesByArea.TryGetValue(a.IdArea, out var areaTables)
                                ? areaTables
                                : new List<TableCardViewModel>()
                        })
                        .ToList()
                })
                .ToList();

            var orphanTables = tables
                .Where(t => !t.AreaId.HasValue || !areaIdSet.Contains(t.AreaId.Value))
                .ToList();

            if (orphanTables.Any())
            {
                groups.Add(new BranchGroupViewModel
                {
                    BranchId = null,
                    BranchName = "Bàn chưa gán khu vực",
                    Areas = new List<AreaGroupViewModel>
                    {
                        new AreaGroupViewModel
                        {
                            AreaId = null,
                            AreaName = "Chưa gán khu vực",
                            Tables = orphanTables.OrderBy(t => t.TableName).ToList()
                        }
                    }
                });
            }

            return groups;
        }

        private TableCreateFormOptions BuildCreateOptions(
            List<tblBranch> branches,
            List<tblArea> areas,
            List<tblTable_type> types,
            List<TableStatusOption> statuses)
        {
            var branchOptions = branches
                .OrderBy(b => b.BranchName)
                .Select(b => new SelectOption
                {
                    Id = b.IdBranch,
                    Name = string.IsNullOrWhiteSpace(b.BranchName) ? $"Chi nhánh #{b.IdBranch}" : b.BranchName!
                })
                .ToList();

            var areaOptions = areas
                .OrderBy(a => a.AreaName)
                .Select(a => new AreaOption
                {
                    Id = a.IdArea,
                    Name = string.IsNullOrWhiteSpace(a.AreaName) ? $"Khu #{a.IdArea}" : a.AreaName!,
                    BranchId = a.IdBranch
                })
                .ToList();

            var typeOptions = types
                .OrderBy(t => t.TypeName)
                .Select(t => new TableTypeOption
                {
                    Id = t.IdType,
                    Name = string.IsNullOrWhiteSpace(t.TypeName) ? $"Loại #{t.IdType}" : t.TypeName!,
                    MaxSeats = t.MaxSeats,
                    Description = t.Description
                })
                .ToList();

            return new TableCreateFormOptions
            {
                Branches = branchOptions,
                Areas = areaOptions,
                Types = typeOptions,
                Statuses = statuses
            };
        }

        private static string ResolveBadgeClass(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                return "bg-secondary";
            }

            var normalized = statusName.ToLowerInvariant();
            if (normalized.Contains("trống") || normalized.Contains("available"))
            {
                return "bg-success";
            }

            if (normalized.Contains("đặt") || normalized.Contains("chờ"))
            {
                return "bg-warning text-dark";
            }

            if (normalized.Contains("khách") || normalized.Contains("phục vụ") || normalized.Contains("occupied"))
            {
                return "bg-danger";
            }

            if (normalized.Contains("bảo trì") || normalized.Contains("maintenance"))
            {
                return "bg-dark";
            }

            return "bg-primary";
        }
    }
}
