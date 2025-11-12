using prog7212_poe_part1_V1.Models;
using prog7212_poe_part1_V1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace prog7212_poe_part1_V1.Services
{
    public class EventService
    {
        private readonly ApplicationDbContext _context;
        private Stack<EventSearch> recentSearches;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
            recentSearches = new Stack<EventSearch>();
        }

        public void AddEvent(Event eventItem)
        {
            _context.Events.Add(eventItem);
            _context.SaveChanges();
        }

        public void UpdateEvent(Event eventItem)
        {
            _context.Events.Update(eventItem);
            _context.SaveChanges();
        }

        public void DeleteEvent(int id)
        {
            var eventItem = _context.Events.Find(id);
            if (eventItem != null)
            {
                _context.Events.Remove(eventItem);
                _context.SaveChanges();
            }
        }

        public Event GetEventById(int id)
        {
            return _context.Events.Find(id);
        }

        public List<Event> SearchEvents(EventSearch search)
        {
            // Only add to recent searches if there's actual search criteria
            if (search != null && (!string.IsNullOrEmpty(search.Category) ||
                !string.IsNullOrEmpty(search.SearchTerm) ||
                search.StartDate.HasValue ||
                search.EndDate.HasValue))
            {
                recentSearches.Push(search);
                UpdateUserPreferences(search);
            }

            // Start with upcoming events only
            var results = _context.Events
                .Where(e => e.Date >= DateTime.Now)
                .AsQueryable();

            // Apply filters if search criteria exists
            if (search != null)
            {
                // Filter by category - FIXED: Check for non-empty and not "All"
                if (!string.IsNullOrEmpty(search.Category) &&
                    search.Category != "All" &&
                    search.Category.Trim() != "")
                {
                    results = results.Where(e => e.Category == search.Category);
                }

                // Filter by start date
                if (search.StartDate.HasValue)
                {
                    results = results.Where(e => e.Date >= search.StartDate.Value);
                }

                // Filter by end date
                if (search.EndDate.HasValue)
                {
                    results = results.Where(e => e.Date <= search.EndDate.Value);
                }

                // Filter by search term (search in title and description)
                if (!string.IsNullOrEmpty(search.SearchTerm) && search.SearchTerm.Trim() != "")
                {
                    var searchTerm = search.SearchTerm.Trim().ToLower();
                    results = results.Where(e =>
                        e.Title.ToLower().Contains(searchTerm) ||
                        e.Description.ToLower().Contains(searchTerm) ||
                        e.Location.ToLower().Contains(searchTerm));
                }
            }

            // Order by priority (lower number = higher priority) then by date
            return results
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.Date)
                .ToList();
        }

        private void UpdateUserPreferences(EventSearch search)
        {
            if (!string.IsNullOrEmpty(search.Category) &&
                search.Category != "All" &&
                search.Category.Trim() != "")
            {
                var preference = _context.UserPreferences
                    .FirstOrDefault(up => up.Category == search.Category);

                if (preference != null)
                {
                    preference.SearchCount++;
                    preference.LastSearched = DateTime.Now;
                }
                else
                {
                    _context.UserPreferences.Add(new UserPreference
                    {
                        Category = search.Category,
                        SearchCount = 1,
                        LastSearched = DateTime.Now
                    });
                }
                _context.SaveChanges();
            }
        }

        public List<Event> GetRecommendedEvents()
        {
            var recommendations = new List<Event>();
            var allEvents = GetUpcomingEvents();

            // Get top 3 most searched categories
            var topCategories = _context.UserPreferences
                .OrderByDescending(up => up.SearchCount)
                .ThenByDescending(up => up.LastSearched)
                .Take(3)
                .Select(up => up.Category)
                .ToList();

            // Get events from top categories
            foreach (var category in topCategories)
            {
                var categoryEvents = allEvents
                    .Where(e => e.Category == category)
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.Date)
                    .Take(2);

                recommendations.AddRange(categoryEvents);
            }

            // If no recommendations based on preferences, show high priority upcoming events
            if (!recommendations.Any())
            {
                recommendations = allEvents
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.Date)
                    .Take(4)
                    .ToList();
            }

            return recommendations.Distinct().ToList();
        }

        public Stack<EventSearch> GetRecentSearches()
        {
            return new Stack<EventSearch>(recentSearches.Reverse());
        }

        public HashSet<string> GetCategories()
        {
            return _context.Events
                .Select(e => e.Category)
                .Distinct()
                .ToHashSet();
        }

        public HashSet<string> GetLocations()
        {
            return _context.Events
                .Select(e => e.Location)
                .Distinct()
                .ToHashSet();
        }

        public List<Event> GetAllEvents()
        {
            return _context.Events
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.Date)
                .ToList();
        }

        public List<Event> GetUpcomingEvents()
        {
            return _context.Events
                .Where(e => e.Date >= DateTime.Now)
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.Date)
                .ToList();
        }

        // New method: Get events by category
        public List<Event> GetEventsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "All")
            {
                return GetUpcomingEvents();
            }

            return _context.Events
                .Where(e => e.Date >= DateTime.Now && e.Category == category)
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.Date)
                .ToList();
        }
    }
}