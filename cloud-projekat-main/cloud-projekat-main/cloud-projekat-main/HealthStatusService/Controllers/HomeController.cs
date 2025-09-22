using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Common;

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
            string diagnostics = "";
            var startTime = DateTime.UtcNow;

            try
            {
                // DETALJNU DIJAGNOSTIKU
                diagnostics += "🔍 Pokretanje Dashboard-a...\n";
                diagnostics += $"⏰ Start time: {startTime:HH:mm:ss.fff}\n";

                // Proveri Azure Storage konekciju
                diagnostics += "📡 Testiranje Azure Storage konekcije...\n";

                if (_healthCheckTable == null)
                {
                    diagnostics += "❌ _healthCheckTable je NULL!\n";
                    diagnostics += "🔧 Uzrok: CloudTable referenca nije kreirana\n";
                    diagnostics += "💡 Rešenje: Proveri connection string u Web.config\n";
                }
                else
                {
                    diagnostics += "✅ _healthCheckTable je kreiran\n";
                    diagnostics += $"📊 Table name: {_healthCheckTable.Name}\n";
                    diagnostics += $"🔗 Table URI: {_healthCheckTable.Uri}\n";
                }

                // Pokušaj da učitaš prave podatke iz Azure Storage
                diagnostics += "📥 Pokušavam da učitam podatke (max 5 sekundi timeout)...\n";
                var queryStartTime = DateTime.UtcNow;

                var realData = await TryLoadRealHealthData();

                var queryEndTime = DateTime.UtcNow;
                var queryDuration = (queryEndTime - queryStartTime).TotalSeconds;
                diagnostics += $"⏱️ Azure Storage query vreme: {queryDuration:F2} sekundi\n";

                if (realData != null)
                {
                    diagnostics += "✅ Uspešno učitani pravi podaci iz Azure Storage!\n";
                    ViewBag.DataSource = "Azure Storage (Real-time podaci)";
                    ViewBag.Diagnostics = diagnostics;
                    return View(realData);
                }

                diagnostics += "⚠️ Nema pravih podataka ili timeout, prebacujem na test podatke\n";
                diagnostics += "💡 Razlog: Možda HealthMonitoringService nije kreirao podatke ili je Azure Storage spor\n";

                // Ako nema pravih podataka, kreiraj test podatke
                var testData = CreateTestHealthData();
                ViewBag.DataSource = "Test podaci (Azure Storage nedostupan ili prazan)";
                ViewBag.Warning = "Azure Storage nedostupan, prazan, ili spor. Prikazuju se test podaci za demonstraciju.";
                ViewBag.Diagnostics = diagnostics;

                var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
                ViewBag.Diagnostics += $"⏱️ Ukupno vreme izvršavanja: {totalTime:F2} sekundi\n";

                return View(testData);
            }
            catch (Exception ex)
            {
                var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
                diagnostics += $"❌ GREŠKA: {ex.Message}\n";
                diagnostics += $"📍 Stack trace: {ex.StackTrace}\n";
                diagnostics += $"⏱️ Ukupno vreme do greške: {totalTime:F2} sekundi\n";

                // U slučaju bilo kakve greške, ipak pokušaj sa test podacima
                var testData = CreateTestHealthData();
                ViewBag.DataSource = "Test podaci (Fallback due to error)";
                ViewBag.Error = $"Greska pri pristupu podacima: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                ViewBag.Diagnostics = diagnostics;

                return View(testData);
            }
        }

        private async Task<HealthStatusViewModel> TryLoadRealHealthData()
        {
            try
            {
                // Proveri da li je tabela inicijalizovana
                if (_healthCheckTable == null)
                {
                    return null;
                }

                // PRVO - pokušaj da učitaš SVE podatke iz HealthCheck tabele da vidimo šta imamo
                var allQuery = new TableQuery<HealthCheckEntity>().Take(1000); // Više podataka za debug
                var allResult = await _healthCheckTable.ExecuteQuerySegmentedAsync(allQuery, null);
                var allHealthChecks = allResult.Results.ToList();

                ViewBag.Diagnostics += $"📊 UKUPNO zdravstvenih provera u tabeli: {allHealthChecks.Count}\n";

                if (allHealthChecks.Any())
                {
                    ViewBag.Diagnostics += $"🕐 Najstariji zapis: {allHealthChecks.Min(h => h.Timestamp):yyyy-MM-dd HH:mm:ss} UTC\n";
                    ViewBag.Diagnostics += $"🕐 Najnoviji zapis: {allHealthChecks.Max(h => h.Timestamp):yyyy-MM-dd HH:mm:ss} UTC\n";

                    var serviceGroups = allHealthChecks.GroupBy(h => h.ServiceName).ToList();
                    ViewBag.Diagnostics += $"📋 Servisi u tabeli:\n";
                    foreach(var group in serviceGroups)
                    {
                        ViewBag.Diagnostics += $"   - {group.Key}: {group.Count()} zapisa\n";
                    }
                }

                // BRŽA PROVALA - ograniči rezultate i dodaj timeout
                var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
                var filter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, twoHoursAgo);

                var query = new TableQuery<HealthCheckEntity>()
                    .Where(filter)
                    .Take(100); // Ograniči na 100 rezultata za brzinu

                ViewBag.Diagnostics += $"🔍 Tražim podatke novije od: {twoHoursAgo:yyyy-MM-dd HH:mm:ss} UTC\n";

                // Dodaj CancellationToken sa timeout od 5 sekundi
                var cancellationTokenSource = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));

                var result = await _healthCheckTable.ExecuteQuerySegmentedAsync(query, null, null, null, cancellationTokenSource.Token);
                var healthChecks = result.Results.ToList();

                ViewBag.Diagnostics += $"📈 Pronađeno {healthChecks.Count} relevantnih zapisa (poslednja 2 sata)\n";

                // Proveri da li ima podataka
                if (!healthChecks.Any())
                {
                    ViewBag.Diagnostics += "⚠️ Nema podataka za poslednja 2 sata - koristićemo sve dostupne podatke\n";

                    // Ako nema podataka za poslednja 2 sata, uzmi sve što imamo
                    if (allHealthChecks.Any())
                    {
                        healthChecks = allHealthChecks.OrderByDescending(h => h.Timestamp).Take(100).ToList();
                        ViewBag.Diagnostics += $"✅ Koristim {healthChecks.Count} najnovijih zapisa iz ukupno {allHealthChecks.Count}\n";
                    }
                    else
                    {
                        return null;
                    }
                }

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

                var timeRange = healthChecks.Any() ?
                    $"Podaci od {healthChecks.Min(h => h.Timestamp):HH:mm} do {healthChecks.Max(h => h.Timestamp):HH:mm} UTC" :
                    $"Poslednja 2 sata (od {twoHoursAgo:HH:mm} do {DateTime.UtcNow:HH:mm} UTC)";

                return new HealthStatusViewModel
                {
                    ServiceStats = serviceStats,
                    OverallAvailability = overallAvailability,
                    TotalChecks = totalChecks,
                    TimeRange = timeRange
                };
            }
            catch (OperationCanceledException)
            {
                // Timeout - vrati null da se koriste test podaci
                ViewBag.Diagnostics += "⏱️ TIMEOUT - Azure Storage predugo odgovara\n";
                return null;
            }
            catch (System.TimeoutException)
            {
                // Timeout - vrati null da se koriste test podaci
                ViewBag.Diagnostics += "⏱️ TIMEOUT - Azure Storage predugo odgovara\n";
                return null;
            }
            catch (Exception ex)
            {
                ViewBag.Diagnostics += $"❌ GREŠKA u TryLoadRealHealthData: {ex.Message}\n";
                return null;
            }
        }

        private HealthStatusViewModel CreateTestHealthData()
        {
            var random = new Random();
            var services = new[] { "MovieDiscussionService", "NotificationService", "HealthStatusService", "AdminToolsConsoleApp" };

            var serviceStats = services.Select(serviceName =>
            {
                var serviceChecks = random.Next(50, 100);
                var serviceSuccessful = random.Next((int)(serviceChecks * 0.7), serviceChecks);
                var serviceFailed = serviceChecks - serviceSuccessful;

                return new ServiceHealthViewModel
                {
                    ServiceName = serviceName,
                    TotalChecks = serviceChecks,
                    SuccessfulChecks = serviceSuccessful,
                    FailedChecks = serviceFailed,
                    AvailabilityPercentage = serviceChecks > 0 ? (double)serviceSuccessful / serviceChecks * 100 : 0,
                    LastCheck = DateTime.UtcNow.AddMinutes(-random.Next(1, 30)),
                    LastStatus = serviceSuccessful > serviceFailed ? "OK" : "NOT_OK"
                };
            }).ToList();

            var totalChecks = serviceStats.Sum(s => s.TotalChecks);
            var totalSuccessful = serviceStats.Sum(s => s.SuccessfulChecks);
            var overallAvailability = totalChecks > 0 ? (double)totalSuccessful / totalChecks * 100 : 0;

            return new HealthStatusViewModel
            {
                ServiceStats = serviceStats,
                OverallAvailability = overallAvailability,
                TotalChecks = totalChecks,
                TimeRange = "Test podaci - simulacija poslednja 2 sata"
            };
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

        // Test endpoint za Dashboard
        [HttpGet]
        public ActionResult TestDashboard()
        {
            try
            {
                var testModel = new HealthStatusViewModel
                {
                    ServiceStats = new List<ServiceHealthViewModel>
                    {
                        new ServiceHealthViewModel
                        {
                            ServiceName = "MovieDiscussionService",
                            TotalChecks = 10,
                            SuccessfulChecks = 8,
                            FailedChecks = 2,
                            AvailabilityPercentage = 80.0,
                            LastCheck = DateTime.UtcNow.AddMinutes(-5),
                            LastStatus = "OK"
                        },
                        new ServiceHealthViewModel
                        {
                            ServiceName = "NotificationService",
                            TotalChecks = 10,
                            SuccessfulChecks = 9,
                            FailedChecks = 1,
                            AvailabilityPercentage = 90.0,
                            LastCheck = DateTime.UtcNow.AddMinutes(-3),
                            LastStatus = "OK"
                        }
                    },
                    OverallAvailability = 85.0,
                    TotalChecks = 20,
                    TimeRange = "Test podaci - poslednja 2 sata"
                };

                return View("Dashboard", testModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Greška u test Dashboard-u: " + ex.Message;
                return View("Dashboard", new HealthStatusViewModel());
            }
        }

        // CREATE TEST DATA endpoint - kreira test podatke u Azure Storage
        [HttpGet]
        public async Task<ActionResult> CreateTestData()
        {
            try
            {
                if (_healthCheckTable == null)
                {
                    return Content("❌ Azure Storage konekcija nije konfigurisana.");
                }

                // Kreiraj tabelu ako ne postoji
                await _healthCheckTable.CreateIfNotExistsAsync();

                var now = DateTime.UtcNow;
                var services = new[] { "MovieDiscussionService", "NotificationService", "HealthStatusService" };
                var insertCount = 0;

                // Kreiraj test podatke za poslednja 4 sata
                for (int i = 0; i < 120; i++) // 120 zapisa (40 po servisu)
                {
                    var timestamp = now.AddMinutes(-i * 2); // Svake 2 minute unazad
                    var serviceName = services[i % services.Length];
                    var isOk = new Random(i).Next(0, 100) > 10; // 90% uspešnih

                    var healthCheck = new HealthCheckEntity("HealthCheck", $"{timestamp:yyyyMMddHHmmss}_{serviceName}_{i}")
                    {
                        ServiceName = serviceName,
                        Status = isOk ? "OK" : "NOT_OK",
                        Timestamp = timestamp
                    };

                    var insertOperation = TableOperation.InsertOrReplace(healthCheck);
                    await _healthCheckTable.ExecuteAsync(insertOperation);
                    insertCount++;
                }

                return Content($"✅ Uspešno kreirano {insertCount} test zapisa za health monitoring!\n" +
                              $"📋 Raspored: {insertCount / 3} zapisa po servisu\n" +
                              $"⏰ Vremenski opseg: {now.AddHours(-4):yyyy-MM-dd HH:mm} do {now:yyyy-MM-dd HH:mm} UTC\n\n" +
                              $"🎯 Sada možeš testirati kompletan dashboard na: /Home/Dashboard");
            }
            catch (Exception ex)
            {
                return Content($"❌ GREŠKA pri kreiranju test podataka: {ex.Message}\n" +
                              $"📍 Stack trace: {ex.StackTrace}");
            }
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

}











