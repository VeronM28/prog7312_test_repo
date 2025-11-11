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
            recentSearches.Push(search);
            UpdateUserPreferences(search);

            var results = _context.Events.AsQueryable();

            if (!string.IsNullOrEmpty(search.Category))
                results = results.Where(e => e.Category == search.Category);

            if (search.StartDate.HasValue)
                results = results.Where(e => e.Date >= search.StartDate.Value);

            if (search.EndDate.HasValue)
                results = results.Where(e => e.Date <= search.EndDate.Value);

            if (!string.IsNullOrEmpty(search.SearchTerm))
                results = results.Where(e => e.Title.Contains(search.SearchTerm) ||
                                           e.Description.Contains(search.SearchTerm));

            return results.OrderBy(e => e.Date).ToList();
        }

        private void UpdateUserPreferences(EventSearch search)
        {
            if (!string.IsNullOrEmpty(search.Category))
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

            var topCategories = _context.UserPreferences
                .OrderByDescending(up => up.SearchCount)
                .ThenByDescending(up => up.LastSearched)
                .Take(3)
                .Select(up => up.Category)
                .ToList();

            foreach (var category in topCategories)
            {
                var categoryEvents = allEvents
                    .Where(e => e.Category == category)
                    .Take(2);

                recommendations.AddRange(categoryEvents);
            }

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
            return _context.Events.OrderBy(e => e.Date).ToList();
        }

        public List<Event> GetUpcomingEvents()
        {
            return _context.Events
                .Where(e => e.Date >= DateTime.Now)
                .OrderBy(e => e.Date)
                .ToList();
        }
    }
}