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

        public IActionResult Index()
        {
            var viewModel = new EventsViewModel
            {
                UpcomingEvents = _eventService.GetUpcomingEvents(),
                Categories = _eventService.GetCategories().ToList(),
                Recommendations = _eventService.GetRecommendedEvents(),
                RecentSearches = _eventService.GetRecentSearches().Take(5).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Search(EventSearch search)
        {
            var results = _eventService.SearchEvents(search);
            var viewModel = new EventsViewModel
            {
                UpcomingEvents = results,
                Categories = _eventService.GetCategories().ToList(),
                SearchCriteria = search,
                Recommendations = _eventService.GetRecommendedEvents(),
                RecentSearches = _eventService.GetRecentSearches().Take(5).ToList()
            };
            return View("Index", viewModel);
        }

        public IActionResult Details(int id)
        {
            var eventItem = _eventService.GetEventById(id);
            if (eventItem == null)
            {
                return NotFound();
            }
            return View(eventItem);
        }

        public JsonResult GetCategories()
        {
            var categories = _eventService.GetCategories().ToList();
            return Json(categories);
        }
    }

    public class EventsViewModel
    {
        public List<Event> UpcomingEvents { get; set; }
        public List<string> Categories { get; set; }
        public EventSearch SearchCriteria { get; set; }
        public List<Event> Recommendations { get; set; }
        public List<EventSearch> RecentSearches { get; set; }
    }
}