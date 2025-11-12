using prog7212_poe_part1_V1.Models;

namespace prog7212_poe_part1_V1.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Report Statistics
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int InProgressReports { get; set; }
        public int CompletedReports { get; set; }
        public List<ReportModel> RecentReports { get; set; } = new List<ReportModel>();
        public List<CategoryStatistic> CategoryStatistics { get; set; } = new List<CategoryStatistic>();

        // Service Request Statistics
        public int TotalServiceRequests { get; set; }
        public int SubmittedServiceRequests { get; set; }
        public int InProgressServiceRequests { get; set; }
        public int UnderReviewServiceRequests { get; set; }
        public int CompletedServiceRequests { get; set; }
        public int CancelledServiceRequests { get; set; }
        public List<ServiceRequestModel> RecentServiceRequests { get; set; } = new List<ServiceRequestModel>();
        public List<ServiceTypeStatistic> ServiceTypeStatistics { get; set; } = new List<ServiceTypeStatistic>();

        // Combined Statistics
        public int TotalItems => TotalReports + TotalServiceRequests;
        public double CompletionRate
        {
            get
            {
                var totalCompleted = CompletedReports + CompletedServiceRequests;
                var totalItems = TotalItems;
                return totalItems > 0 ? (double)totalCompleted / totalItems * 100 : 0;
            }
        }

        // Priority Statistics
        public int HighPriorityServiceRequests { get; set; }
        public int MediumPriorityServiceRequests { get; set; }
        public int LowPriorityServiceRequests { get; set; }
    }

    public class CategoryStatistic
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }

    public class ServiceTypeStatistic
    {
        public ServiceType ServiceType { get; set; }
        public string ServiceTypeDisplayName => ServiceType.ToString();
        public int Count { get; set; }
    }

    public class PriorityStatistic
    {
        public string PriorityLevel { get; set; }
        public int Count { get; set; }
    }
}