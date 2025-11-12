// Controllers/ServiceRequestController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using prog7212_poe_part1_V1.Data;
using prog7212_poe_part1_V1.Models;

namespace prog7212_poe_part1_V1.Controllers
{
    [Authorize]
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ServiceRequest/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequestModel serviceRequest)
        {
            // Debug: Check if we're reaching the action
            System.Diagnostics.Debug.WriteLine("=== CREATE ACTION REACHED ===");
            Console.WriteLine("=== CREATE ACTION REACHED ===");

            // Get user ID first
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            System.Diagnostics.Debug.WriteLine($"User ID: {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "User not authenticated properly.");
                return View(serviceRequest);
            }

            // IMPORTANT: Remove UserId from ModelState validation since we're setting it manually
            ModelState.Remove("UserId");

            // Debug: Check ModelState after removal
            System.Diagnostics.Debug.WriteLine($"ModelState IsValid after UserId removal: {ModelState.IsValid}");
            Console.WriteLine($"ModelState IsValid after UserId removal: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== MODEL STATE ERRORS ===");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                        Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
                return View(serviceRequest);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== STARTING DATABASE OPERATION ===");

                // Set properties
                serviceRequest.UserId = userId; // This will now work
                serviceRequest.RequestId = Guid.NewGuid().ToString();
                serviceRequest.CreatedDate = DateTime.Now;
                serviceRequest.Status = RequestStatus.Submitted;
                serviceRequest.PriorityScore = CalculatePriorityScore(serviceRequest);

                System.Diagnostics.Debug.WriteLine($"Request ID: {serviceRequest.RequestId}");
                System.Diagnostics.Debug.WriteLine($"Title: {serviceRequest.Title}");
                System.Diagnostics.Debug.WriteLine($"ServiceType: {serviceRequest.ServiceType}");
                System.Diagnostics.Debug.WriteLine($"UserId set to: {serviceRequest.UserId}");

                // Add to context
                _context.ServiceRequests.Add(serviceRequest);

                // Save changes
                System.Diagnostics.Debug.WriteLine("Attempting to save to database...");
                int recordsAffected = await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Records affected: {recordsAffected}");

                if (recordsAffected > 0)
                {
                    System.Diagnostics.Debug.WriteLine("=== SUCCESS: Record saved ===");

                    TempData["SuccessMessage"] = $"Service request created successfully! Your tracking ID is: {serviceRequest.RequestId}";
                    return RedirectToAction(nameof(Status));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== WARNING: No records affected ===");
                    ModelState.AddModelError("", "No records were saved to the database.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"=== DATABASE EXCEPTION: {dbEx.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Inner: {dbEx.InnerException?.Message}");
                Console.WriteLine($"DB Error: {dbEx.Message}");
                ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERAL EXCEPTION: {ex.Message} ===");
                Console.WriteLine($"General Error: {ex.Message}");
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return View(serviceRequest);
        }

        // GET: ServiceRequest/Status
        public IActionResult Status(string searchId = null)
        {
            if (!string.IsNullOrEmpty(searchId))
            {
                var request = _context.ServiceRequests
                    .FirstOrDefault(r => r.RequestId == searchId);

                if (request != null)
                {
                    return View("StatusDetail", request);
                }
                else
                {
                    ViewBag.ErrorMessage = "No service request found with that ID.";
                }
            }

            // FIX: Use User ID instead of Name for querying
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRequests = _context.ServiceRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            return View(userRequests);
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

        // GET: ServiceRequest/Test
        [AllowAnonymous] // Temporarily allow anonymous for testing
        public IActionResult Test()
        {
            return Content($"ServiceRequestController is working. Time: {DateTime.Now}");
        }

        // GET: ServiceRequest/TestCreate
        [AllowAnonymous] // Temporarily allow anonymous for testing
        public IActionResult TestCreate()
        {
            var testRequest = new ServiceRequestModel
            {
                Title = "Test Request",
                Description = "This is a test description",
                ServiceType = ServiceType.Other,
                Address = "123 Test Street",
                UserId = "test-user-id",
                RequestId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.Now,
                PriorityScore = 50
            };

            try
            {
                _context.ServiceRequests.Add(testRequest);
                _context.SaveChanges();
                return Content($"Test record created successfully! ID: {testRequest.RequestId}");
            }
            catch (Exception ex)
            {
                return Content($"Test failed: {ex.Message}");
            }
        }
    }
}