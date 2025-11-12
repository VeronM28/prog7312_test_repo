// Models/ServiceRequestModel.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace prog7212_poe_part1_V1.Models
{
    public enum RequestStatus
    {
        Submitted,
        InProgress,
        UnderReview,
        Completed,
        Cancelled
    }

    public enum ServiceType
    {
        WasteCollection,
        RoadRepair,
        WaterIssue,
        Electricity,
        Sanitation,
        Other
    }

    public class ServiceRequestModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        // Remove StringLength from primary key
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Submitted;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        public string? AssignedAdmin { get; set; }

        [StringLength(2000)]
        public string? AdminNotes { get; set; }

        // Location information
        [Required]
        [StringLength(500)]
        public string Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Priority calculated based on various factors
        public int PriorityScore { get; set; }

        // Navigation property to UserModel
        [ForeignKey("UserId")]
        public virtual UserModel? User { get; set; }
    }
}