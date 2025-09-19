using System;
namespace Common
{
    public class ServiceHealthViewModel
    {
        public string ServiceName { get; set; }
        public int TotalChecks { get; set; }
        public int SuccessfulChecks { get; set; }
        public int FailedChecks { get; set; }
        public double AvailabilityPercentage { get; set; }
        public DateTime LastCheck { get; set; }
        public string LastStatus { get; set; }
    }
}








