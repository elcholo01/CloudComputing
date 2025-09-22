using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;

namespace HealthStatusService.Controllers
{
    public class HomeController : Controller
    {
        private CloudTable _healthCheckTable;

        public HomeController()
        {
            var storageAccount = CloudStorageAccount.Parse(
                System.Configuration.ConfigurationManager.ConnectionStrings["DataConnectionString"].ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _healthCheckTable = tableClient.GetTableReference("HealthCheck");
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                // Uzmi podatke za poslednja 2 sata
                var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
                var filter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, twoHoursAgo);
                
                var query = new TableQuery<HealthCheckEntity>().Where(filter);
                var result = await _healthCheckTable.ExecuteQuerySegmentedAsync(query, null);
                var healthChecks = result.Results.ToList();

                // Grupiši po servisu i izracunaj dostupnost
                var serviceStats = healthChecks
                    .GroupBy(h => h.ServiceName)
                    .Select(g => new ServiceHealthViewModel
                    {
                        ServiceName = g.Key,
                        TotalChecks = g.Count(),
                        SuccessfulChecks = g.Count(h => h.Status == "OK"),
                        FailedChecks = g.Count(h => h.Status == "NOT_OK"),
                        AvailabilityPercentage = g.Count() > 0 ? (double)g.Count(h => h.Status == "OK") / g.Count() * 100 : 0,
                        LastCheck = g.Max(h => h.Timestamp),
                        LastStatus = g.OrderByDescending(h => h.Timestamp).First().Status
                    })
                    .ToList();

                // Izracunaj ukupnu dostupnost
                var totalChecks = healthChecks.Count;
                var totalSuccessful = healthChecks.Count(h => h.Status == "OK");
                var overallAvailability = totalChecks > 0 ? (double)totalSuccessful / totalChecks * 100 : 0;

                var viewModel = new HealthStatusViewModel
                {
                    ServiceStats = serviceStats,
                    OverallAvailability = overallAvailability,
                    TotalChecks = totalChecks,
                    TimeRange = $"Poslednja 2 sata (od {twoHoursAgo:HH:mm} do {DateTime.UtcNow:HH:mm} UTC)"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Greška prilikom ucitavanja podataka o zdravlju servisa: " + ex.Message;
                return View(new HealthStatusViewModel());
            }
        }

        // Health monitoring endpoint
        [HttpGet]
        [Route("health-monitoring")]
        public ActionResult HealthMonitoring()
        {
            // Jednostavan health check endpoint
            // Vraća OK status ako servis radi
            return new HttpStatusCodeResult(200, "OK - HealthStatusService is healthy");
        }

        // API endpoint za AJAX pozive
        [HttpGet]
        public async Task<JsonResult> GetHealthData()
        {
            try
            {
                var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
                var filter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, twoHoursAgo);
                
                var query = new TableQuery<HealthCheckEntity>().Where(filter);
                var result = await _healthCheckTable.ExecuteQuerySegmentedAsync(query, null);
                var healthChecks = result.Results.ToList();

                // Grupiši po vremenu za grafik
                var timeSeriesData = healthChecks
                    .GroupBy(h => new { Date = h.Timestamp.Date, Hour = h.Timestamp.Hour, Minute = h.Timestamp.Minute / 10 * 10 })
                    .Select(g => new
                    {
                        Time = g.Key.Date.AddHours(g.Key.Hour).AddMinutes(g.Key.Minute),
                        Services = g.GroupBy(h => h.ServiceName)
                            .Select(s => new
                            {
                                ServiceName = s.Key,
                                Status = s.OrderByDescending(h => h.Timestamp).First().Status
                            }).ToList()
                    })
                    .OrderBy(x => x.Time)
                    .ToList();

                return Json(timeSeriesData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }

    public class HealthStatusViewModel
    {
        public List<ServiceHealthViewModel> ServiceStats { get; set; }
        public double OverallAvailability { get; set; }
        public int TotalChecks { get; set; }
        public string TimeRange { get; set; }
    }

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

    public class HealthCheckEntity : TableEntity
    {
        public string Status { get; set; }
        public string ServiceName { get; set; }
        public new DateTime Timestamp { get; set; }
    }
}











