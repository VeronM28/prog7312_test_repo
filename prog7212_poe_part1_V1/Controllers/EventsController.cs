using Microsoft.AspNetCore.Mvc;
using prog7212_poe_part1_V1.Models;
using prog7212_poe_part1_V1.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace prog7212_poe_part1_V1.Controllers
{
    public class EventsController : Controller
    {
        private readonly EventService _eventService;

        public EventsController(EventService eventService)
        {
            _eventService = eventService;
        }

        // GET: Events
        public IActionResult Index(string category = "", string searchTerm = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            List<Event> events;

            // Check if any filter is applied
            bool hasFilters = !string.IsNullOrEmpty(category) ||
                            !string.IsNullOrEmpty(searchTerm) ||
                            startDate.HasValue ||
                            endDate.HasValue;

            if (hasFilters)
            {
                // Create search object
                var search = new EventSearch
                {
                    Category = category,
                    SearchTerm = searchTerm,
                    StartDate = startDate,
                    EndDate = endDate
                };

                // Get filtered events
                events = _eventService.SearchEvents(search);

                var viewModel = new EventsViewModel
                {
                    UpcomingEvents = events,
                    Categories = _eventService.GetCategories().ToList(),
                    SearchCriteria = search,
                    Recommendations = _eventService.GetRecommendedEvents(),
                    RecentSearches = _eventService.GetRecentSearches().Take(5).ToList()
                };

                return View(viewModel);
            }
            else
            {
                // No filters - show all upcoming events
                events = _eventService.GetUpcomingEvents();

                var viewModel = new EventsViewModel
                {
                    UpcomingEvents = events,
                    Categories = _eventService.GetCategories().ToList(),
                    SearchCriteria = new EventSearch(),
                    Recommendations = _eventService.GetRecommendedEvents(),
                    RecentSearches = _eventService.GetRecentSearches().Take(5).ToList()
                };

                return View(viewModel);
            }
        }

        // POST: Events/Search
        [HttpPost]
        public IActionResult Search(EventSearch search)
        {
            // Redirect to Index with query parameters to maintain URL consistency
            return RedirectToAction("Index", new
            {
                category = search.Category ?? "",
                searchTerm = search.SearchTerm ?? "",
                startDate = search.StartDate,
                endDate = search.EndDate
            });
        }

        // GET: Events/Details/5
        public IActionResult Details(int id)
        {
            var eventItem = _eventService.GetEventById(id);
            if (eventItem == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index");
            }
            return View(eventItem);
        }

        // AJAX endpoint to get categories
        [HttpGet]
        public JsonResult GetCategories()
        {
            var categories = _eventService.GetCategories().OrderBy(c => c).ToList();
            return Json(categories);
        }

        // AJAX endpoint to filter events by category
        [HttpGet]
        public IActionResult FilterByCategory(string category)
        {
            List<Event> events;

            if (string.IsNullOrEmpty(category) || category == "All")
            {
                events = _eventService.GetUpcomingEvents();
            }
            else
            {
                var search = new EventSearch { Category = category };
                events = _eventService.SearchEvents(search);
            }

            return PartialView("_EventsList", events);
        }

        // Clear all filters
        [HttpGet]
        public IActionResult ClearFilters()
        {
            return RedirectToAction("Index");
        }
    }

    public class EventsViewModel
    {
        public List<Event> UpcomingEvents { get; set; } = new List<Event>();
        public List<string> Categories { get; set; } = new List<string>();
        public EventSearch SearchCriteria { get; set; } = new EventSearch();
        public List<Event> Recommendations { get; set; } = new List<Event>();
        public List<EventSearch> RecentSearches { get; set; } = new List<EventSearch>();
    }
}