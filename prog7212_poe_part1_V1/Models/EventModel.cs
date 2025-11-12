using System;
using System.ComponentModel.DataAnnotations;

namespace prog7212_poe_part1_V1.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Event Date & Time")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-ddTHH:mm}")]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Range(0, 10000)]
        public decimal Price { get; set; }

        [Range(1, 5)]
        public int Priority { get; set; } = 3; // Default priority

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; } // Admin who created it
    }

    public class EventSearch
    {
        public string Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SearchTerm { get; set; }
    }

    public class UserPreference
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Category { get; set; }
        public int SearchCount { get; set; }
        public DateTime LastSearched { get; set; }
    }
}