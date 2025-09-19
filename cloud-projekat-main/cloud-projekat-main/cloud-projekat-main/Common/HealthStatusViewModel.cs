using System.Collections.Generic;
namespace Common
{
    public class HealthStatusViewModel
    {
        public List<ServiceHealthViewModel> ServiceStats { get; set; } = new List<ServiceHealthViewModel>();
        public double OverallAvailability { get; set; }
        public int TotalChecks { get; set; }
        public string TimeRange { get; set; }
    }
}








