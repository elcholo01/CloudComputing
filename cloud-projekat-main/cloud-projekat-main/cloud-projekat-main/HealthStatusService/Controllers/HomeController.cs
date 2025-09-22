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

        public ActionResult Index()
        {
            // Jednostavan Index bez Azure zavisnosti
            ViewBag.Title = "Health Status Service - RADI!";
            ViewBag.Message = "Web aplikacija je uspešno pokrenuta!";
            ViewBag.CurrentTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            return View();
        }

        public async Task<ActionResult> Dashboard()
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

        // API endpoint za AJAX pozive - Jednostavan JSON bez Newtonsoft zavisnosti
        [HttpGet]
        public async Task<ActionResult> GetHealthData()
        {
            try
            {
                var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
                var filter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, twoHoursAgo);

                var query = new TableQuery<HealthCheckEntity>().Where(filter);
                var result = await _healthCheckTable.ExecuteQuerySegmentedAsync(query, null);
                var healthChecks = result.Results.ToList();

                // Jednostavan string JSON odziv
                var jsonResponse = "[";
                var dataPoints = healthChecks
                    .GroupBy(h => new { Date = h.Timestamp.Date, Hour = h.Timestamp.Hour, Minute = h.Timestamp.Minute / 10 * 10 })
                    .OrderBy(g => g.Key.Date.AddHours(g.Key.Hour).AddMinutes(g.Key.Minute))
                    .Take(20) // Ograniči na poslednja 20 rezultata
                    .ToList();

                for (int i = 0; i < dataPoints.Count; i++)
                {
                    var g = dataPoints[i];
                    var time = g.Key.Date.AddHours(g.Key.Hour).AddMinutes(g.Key.Minute);

                    jsonResponse += "{";
                    jsonResponse += $"\"Time\":\"{time:yyyy-MM-ddTHH:mm:ss}\",";
                    jsonResponse += "\"Services\":[";

                    var services = g.GroupBy(h => h.ServiceName).ToList();
                    for (int j = 0; j < services.Count; j++)
                    {
                        var service = services[j];
                        var status = service.OrderByDescending(h => h.Timestamp).First().Status;
                        jsonResponse += $"{{\"ServiceName\":\"{service.Key}\",\"Status\":\"{status}\"}}";
                        if (j < services.Count - 1) jsonResponse += ",";
                    }

                    jsonResponse += "]}";
                    if (i < dataPoints.Count - 1) jsonResponse += ",";
                }

                jsonResponse += "]";

                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                return Content($"{{\"error\":\"{ex.Message}\"}}", "application/json");
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











