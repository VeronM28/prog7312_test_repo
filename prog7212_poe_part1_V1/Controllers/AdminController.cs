using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prog7212_poe_part1_V1.Data;
using prog7212_poe_part1_V1.Models;
using prog7212_poe_part1_V1.ViewModels;
using prog7212_poe_part1_V1.Services;
using prog7212_poe_part1_V1.DataStructures;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace prog7212_poe_part1_V1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<UserModel> _userManager;
        private readonly EventService _eventService;
        private readonly RequestBST _requestBST;
        private readonly RequestPriorityQueue _priorityQueue;
        private readonly ServiceAreaGraph _serviceAreaGraph;

        public AdminController(ApplicationDbContext context, UserManager<UserModel> userManager, EventService eventService)
        {
            _context = context;
            _userManager = userManager;
            _eventService = eventService;
            _requestBST = new RequestBST();
            _priorityQueue = new RequestPriorityQueue();
            _serviceAreaGraph = new ServiceAreaGraph();

            InitializeDataStructures();
        }

        private void InitializeDataStructures()
        {
            // Load existing requests into data structures
            var requests = _context.ServiceRequests.ToList();
            foreach (var request in requests)
            {
                _requestBST.Insert(request);
                _priorityQueue.Enqueue(request);
            }

            // Initialize service areas (this would typically come from a database)
            InitializeServiceAreas();
        }

        private void InitializeServiceAreas()
        {
            // Add predefined service areas
            _serviceAreaGraph.AddArea("Downtown", 40.7128, -74.0060);
            _serviceAreaGraph.AddArea("Uptown", 40.7812, -73.9665);
            _serviceAreaGraph.AddArea("Eastside", 40.7282, -73.9842);
            _serviceAreaGraph.AddArea("Westside", 40.7870, -73.9754);

            // Connect areas based on geographical proximity
            _serviceAreaGraph.AddConnection("Downtown", "Eastside");
            _serviceAreaGraph.AddConnection("Downtown", "Westside");
            _serviceAreaGraph.AddConnection("Uptown", "Westside");
            _serviceAreaGraph.AddConnection("Uptown", "Eastside");
        }

        // GET: Admin/ServiceRequests
        [Authorize(Roles = "Admin")]
        public IActionResult ServiceRequests()
        {
            try
            {
                var allRequests = _context.ServiceRequests
                    .OrderByDescending(r => r.CreatedDate)
                    .ToList();

                // Get high priority requests from heap - FIXED VERSION
                var highPriorityRequests = new List<ServiceRequestModel>();
                var tempQueue = new RequestPriorityQueue();

                // Add requests to priority queue
                foreach (var request in allRequests.Where(r =>
                    r.Status == RequestStatus.Submitted || r.Status == RequestStatus.InProgress))
                {
                    tempQueue.Enqueue(request);
                }

                // Get top 10 highest priority requests
                int count = Math.Min(10, tempQueue.Count);
                for (int i = 0; i < count; i++)
                {
                    if (tempQueue.Count > 0)
                    {
                        var highPriorityRequest = tempQueue.Dequeue();
                        highPriorityRequests.Add(highPriorityRequest);
                    }
                }

                ViewBag.HighPriorityRequests = highPriorityRequests;
                ViewBag.OptimalRoute = _serviceAreaGraph?.GetRequestsInOptimalOrder("Downtown") ?? new List<ServiceRequestModel>();

                // Debug output
                System.Diagnostics.Debug.WriteLine($"High Priority Requests Count: {highPriorityRequests.Count}");
                foreach (var req in highPriorityRequests)
                {
                    System.Diagnostics.Debug.WriteLine($"Request: {req.RequestId}, Priority: {req.PriorityScore}, Status: {req.Status}");
                }

                return View(allRequests);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error in ServiceRequests: {ex.Message}");
                ViewBag.HighPriorityRequests = new List<ServiceRequestModel>();
                return View(new List<ServiceRequestModel>());
            }
        }

        // POST: Admin/UpdateServiceRequestStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateServiceRequestStatus(string requestId, RequestStatus newStatus, string adminNotes = null)
        {
            try
            {
                var request = await _context.ServiceRequests
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Service request not found.";
                    return RedirectToAction(nameof(ServiceRequests));
                }

                request.Status = newStatus;
                request.UpdatedDate = DateTime.Now;
                request.AdminNotes = adminNotes;
                request.AssignedAdmin = User.Identity.Name;

                // Recalculate priority if needed
                request.PriorityScore = CalculatePriorityScore(request);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Service request status updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(ServiceRequests));
        }

        private int CalculatePriorityScore(ServiceRequestModel request)
        {
            int score = 0;

            // Service type priority
            switch (request.ServiceType)
            {
                case ServiceType.WaterIssue:
                    score += 100;
                    break;
                case ServiceType.Electricity:
                    score += 90;
                    break;
                case ServiceType.WasteCollection:
                    score += 70;
                    break;
                case ServiceType.RoadRepair:
                    score += 60;
                    break;
                case ServiceType.Sanitation:
                    score += 80;
                    break;
                default:
                    score += 50;
                    break;
            }

            // Age of request (older requests get higher priority)
            var ageInDays = (DateTime.Now - request.CreatedDate).TotalDays;
            score += (int)(ageInDays * 10);

            // Status-based priority
            if (request.Status == RequestStatus.Submitted)
                score += 30;
            else if (request.Status == RequestStatus.InProgress)
                score += 20;

            return score;
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
            // Report Statistics
            var totalReports = await _context.Reports.CountAsync();
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending");
            var inProgressReports = await _context.Reports.CountAsync(r => r.Status == "In-Progress");
            var completedReports = await _context.Reports.CountAsync(r => r.Status == "Completed");

            // Service Request Statistics
            var totalServiceRequests = await _context.ServiceRequests.CountAsync();
            var submittedServiceRequests = await _context.ServiceRequests.CountAsync(r => r.Status == RequestStatus.Submitted);
            var inProgressServiceRequests = await _context.ServiceRequests.CountAsync(r => r.Status == RequestStatus.InProgress);
            var underReviewServiceRequests = await _context.ServiceRequests.CountAsync(r => r.Status == RequestStatus.UnderReview);
            var completedServiceRequests = await _context.ServiceRequests.CountAsync(r => r.Status == RequestStatus.Completed);
            var cancelledServiceRequests = await _context.ServiceRequests.CountAsync(r => r.Status == RequestStatus.Cancelled);

            // Priority Statistics for Service Requests
            var highPriorityServiceRequests = await _context.ServiceRequests.CountAsync(r => r.PriorityScore >= 150);
            var mediumPriorityServiceRequests = await _context.ServiceRequests.CountAsync(r => r.PriorityScore >= 100 && r.PriorityScore < 150);
            var lowPriorityServiceRequests = await _context.ServiceRequests.CountAsync(r => r.PriorityScore < 100);

            var recentReports = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.DateSubmitted)
                .Take(5)
                .ToListAsync();

            var recentServiceRequests = await _context.ServiceRequests
                .OrderByDescending(r => r.CreatedDate)
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

            var serviceTypeStats = await _context.ServiceRequests
                .GroupBy(r => r.ServiceType)
                .Select(g => new ServiceTypeStatistic
                {
                    ServiceType = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(sts => sts.Count)
                .ToListAsync();

            var dashboardViewModel = new AdminDashboardViewModel
            {
                // Report Statistics
                TotalReports = totalReports,
                PendingReports = pendingReports,
                InProgressReports = inProgressReports,
                CompletedReports = completedReports,
                RecentReports = recentReports,
                CategoryStatistics = categoryStats,

                // Service Request Statistics
                TotalServiceRequests = totalServiceRequests,
                SubmittedServiceRequests = submittedServiceRequests,
                InProgressServiceRequests = inProgressServiceRequests,
                UnderReviewServiceRequests = underReviewServiceRequests,
                CompletedServiceRequests = completedServiceRequests,
                CancelledServiceRequests = cancelledServiceRequests,
                RecentServiceRequests = recentServiceRequests,
                ServiceTypeStatistics = serviceTypeStats,

                // Priority Statistics
                HighPriorityServiceRequests = highPriorityServiceRequests,
                MediumPriorityServiceRequests = mediumPriorityServiceRequests,
                LowPriorityServiceRequests = lowPriorityServiceRequests
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

        // Temporary debug action
        public IActionResult TestView()
        {
            // Test if we can return the view explicitly
            return View("~/Views/Admin/ServiceRequests.cshtml", new List<ServiceRequestModel>());
        }

    }


   
}