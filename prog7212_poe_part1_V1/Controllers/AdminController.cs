using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prog7212_poe_part1_V1.Data;
using prog7212_poe_part1_V1.Models;
using prog7212_poe_part1_V1.ViewModels;
using prog7212_poe_part1_V1.Services;

namespace prog7212_poe_part1_V1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<UserModel> _userManager;
        private readonly EventService _eventService;

        public AdminController(ApplicationDbContext context, UserManager<UserModel> userManager, EventService eventService)
        {
            _context = context;
            _userManager = userManager;
            _eventService = eventService;
        }

        // GET: Admin/Reports
        public async Task<IActionResult> Reports(string category = "", string status = "")
        {
            var query = _context.Reports.Include(r => r.User).AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query = query.Where(r => r.Category == category);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            var reports = await query.OrderByDescending(r => r.DateSubmitted).ToListAsync();

            var viewModel = new AdminReportsViewModel
            {
                Reports = reports,
                SelectedCategory = category,
                SelectedStatus = status,
                Categories = GetCategoryList(),
                Statuses = GetStatusList()
            };

            return View(viewModel);
        }

        // POST: Admin/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int reportId, string status)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                {
                    TempData["Error"] = "Report not found.";
                    return RedirectToAction("Reports");
                }

                var validStatuses = new[] { "Pending", "In-Progress", "Completed" };
                if (!validStatuses.Contains(status))
                {
                    TempData["Error"] = "Invalid status.";
                    return RedirectToAction("Reports");
                }

                report.Status = status;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Report #{report.Id} status updated to {status}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the status.";
            }

            return RedirectToAction("Reports");
        }

        // GET: Admin/ReportDetails/5
        public async Task<IActionResult> ReportDetails(int id)
        {
            var report = await _context.Reports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                TempData["Error"] = "Report not found.";
                return RedirectToAction("Reports");
            }

            return View(report);
        }

        private List<string> GetCategoryList()
        {
            return new List<string>
            {
                "All",
                "Sanitation",
                "Roads",
                "Utilities",
                "Safety",
                "Environment",
                "Public Transport",
                "Housing",
                "Other"
            };
        }

        private List<string> GetStatusList()
        {
            return new List<string>
            {
                "All",
                "Pending",
                "In-Progress",
                "Completed"
            };
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalReports = await _context.Reports.CountAsync();
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending");
            var inProgressReports = await _context.Reports.CountAsync(r => r.Status == "In-Progress");
            var completedReports = await _context.Reports.CountAsync(r => r.Status == "Completed");

            var recentReports = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.DateSubmitted)
                .Take(5)
                .ToListAsync();

            var categoryStats = await _context.Reports
                .GroupBy(r => r.Category)
                .Select(g => new CategoryStatistic
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(cs => cs.Count)
                .ToListAsync();

            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalReports = totalReports,
                PendingReports = pendingReports,
                InProgressReports = inProgressReports,
                CompletedReports = completedReports,
                RecentReports = recentReports,
                CategoryStatistics = categoryStats
            };

            return View(dashboardViewModel);
        }

        // GET: Admin/Events
        public IActionResult Events()
        {
            var events = _eventService.GetAllEvents();
            return View(events);
        }

        // GET: Admin/CreateEvent
        public IActionResult CreateEvent()
        {
            ViewBag.Categories = GetEventCategoryList();
            // Initialize with default values to help with validation
            var model = new Event
            {
                // Remove seconds and milliseconds from default date
                Date = new DateTime(
                    DateTime.Now.AddDays(1).Year,
                    DateTime.Now.AddDays(1).Month,
                    DateTime.Now.AddDays(1).Day,
                    DateTime.Now.AddDays(1).Hour,
                    DateTime.Now.AddDays(1).Minute,
                    0
                ),
                Priority = 3,
                Price = 0
            };
            return View(model);
        }

        // POST: Admin/CreateEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event eventModel)
        {
            // Remove validation for fields that are auto-populated
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("Id");

            // Additional validation logging for debugging
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Validation failed: " + string.Join(", ", errors);
                ViewBag.Categories = GetEventCategoryList();
                return View(eventModel);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                eventModel.CreatedBy = user?.Email ?? "Admin";
                eventModel.CreatedDate = DateTime.Now;

                // Remove seconds and milliseconds from the date before saving
                eventModel.Date = new DateTime(
                    eventModel.Date.Year,
                    eventModel.Date.Month,
                    eventModel.Date.Day,
                    eventModel.Date.Hour,
                    eventModel.Date.Minute,
                    0
                );

                // Ensure Priority is set
                if (eventModel.Priority < 1 || eventModel.Priority > 5)
                {
                    eventModel.Priority = 3;
                }

                _eventService.AddEvent(eventModel);

                TempData["Success"] = "Event created successfully!";
                return RedirectToAction("Events");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while creating the event: {ex.Message}";
                ViewBag.Categories = GetEventCategoryList();
                return View(eventModel);
            }
        }

        // GET: Admin/EditEvent/5
        public IActionResult EditEvent(int id)
        {
            var eventItem = _eventService.GetEventById(id);
            if (eventItem == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Events");
            }

            ViewBag.Categories = GetEventCategoryList();
            return View(eventItem);
        }

        // POST: Admin/EditEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEvent(Event eventModel)
        {
            // Remove validation for auto-populated fields
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Validation failed: " + string.Join(", ", errors);
                ViewBag.Categories = GetEventCategoryList();
                return View(eventModel);
            }

            try
            {
                _eventService.UpdateEvent(eventModel);
                TempData["Success"] = "Event updated successfully!";
                return RedirectToAction("Events");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while updating the event: {ex.Message}";
                ViewBag.Categories = GetEventCategoryList();
                return View(eventModel);
            }
        }

        // POST: Admin/DeleteEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEvent(int id)
        {
            try
            {
                _eventService.DeleteEvent(id);
                TempData["Success"] = "Event deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deleting the event: {ex.Message}";
            }

            return RedirectToAction("Events");
        }

        private List<string> GetEventCategoryList()
        {
            return new List<string>
            {
                "Music",
                "Technology",
                "Food",
                "Art",
                "Sports",
                "Education",
                "Business",
                "Health",
                "Entertainment",
                "Community",
                "Other"
            };
        }
    }
}