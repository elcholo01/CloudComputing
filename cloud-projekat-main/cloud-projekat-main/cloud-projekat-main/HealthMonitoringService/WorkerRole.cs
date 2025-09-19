using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;

namespace HealthMonitoringService
{
    public class WorkerRole 
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private CloudTable _healthCheckTable;
        private CloudTable _alertEmailsTable;
        private HttpClient _httpClient;

        public void Run()
        {
            Trace.TraceInformation("HealthMonitoringService is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public bool OnStart()
        {
            // Use TLS 1.2 for Service Bus connections
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");

            // Tables
            var tableClient = storageAccount.CreateCloudTableClient();
            _healthCheckTable = tableClient.GetTableReference("HealthCheck");
            _alertEmailsTable = tableClient.GetTableReference("AlertEmails");
            _healthCheckTable.CreateIfNotExists();
            _alertEmailsTable.CreateIfNotExists();

            // HTTP client for health checks
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            Trace.TraceInformation("HealthMonitoringService has been started");

            return true;
        }

        public void OnStop()
        {
            Trace.TraceInformation("HealthMonitoringService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            _httpClient?.Dispose();

            Trace.TraceInformation("HealthMonitoringService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Proveri MovieDiscussionService
                    await CheckServiceHealth("MovieDiscussionService", "http://localhost:8080");

                    // Proveri NotificationService
                    await CheckServiceHealth("NotificationService", "http://localhost:8081");

                    // Čekaj 3 sekunde pre sledeće provere
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Greška u health monitoring servisu: {ex.Message}");
                    await Task.Delay(5000, cancellationToken); // Povećaj delay u slučaju greške
                }
            }
        }

        private async Task CheckServiceHealth(string serviceName, string serviceUrl)
        {
            var healthCheck = new HealthCheckEntity(DateTime.UtcNow, serviceName);
            bool isHealthy = false;

            try
            {
                if (!string.IsNullOrEmpty(serviceUrl))
                {
                    var response = await _httpClient.GetAsync($"{serviceUrl.TrimEnd('/')}/health-monitoring");
                    isHealthy = response.IsSuccessStatusCode;
                    
                    if (!isHealthy)
                    {
                        await SendAlertEmail(serviceName, $"Servis {serviceName} nije dostupan. HTTP Status: {response.StatusCode}");
                    }
                }
                else
                {
                    Trace.TraceWarning($"URL za {serviceName} nije konfigurisan");
                    isHealthy = false;
                }
            }
            catch (Exception ex)
            {
                isHealthy = false;
                Trace.TraceError($"Greška prilikom provere {serviceName}: {ex.Message}");
                await SendAlertEmail(serviceName, $"Greška prilikom provere {serviceName}: {ex.Message}");
            }

            // Loguj rezultat
            healthCheck.Status = isHealthy ? "OK" : "NOT_OK";
            var insertOperation = TableOperation.Insert(healthCheck);
            await _healthCheckTable.ExecuteAsync(insertOperation);

            Trace.TraceInformation($"Health check za {serviceName}: {(isHealthy ? "OK" : "NOT_OK")}");
        }

        private async Task SendAlertEmail(string serviceName, string message)
        {
            try
            {
                // Učitaj sve email adrese za upozorenja
                var query = new TableQuery<AlertEmailEntity>();
                var result = await _alertEmailsTable.ExecuteQuerySegmentedAsync(query, null);
                var alertEmails = result.Results.ToList();

                if (alertEmails.Any())
                {
                    // Ovde bi trebalo implementirati slanje email-a
                    // Za sada samo logujemo
                    Trace.TraceWarning($"ALERT: {message}");
                    Trace.TraceWarning($"Trebalo bi poslati email na: {string.Join(", ", alertEmails.Select(e => e.Email))}");
                }
                else
                {
                    Trace.TraceWarning("Nema konfigurisanih email adresa za upozorenja");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Greška prilikom slanja alert email-a: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            var workerRole = new WorkerRole();
            workerRole.Run();
        }
    }

    public class HealthCheckEntity : TableEntity
    {
        public string Status { get; set; }
        public string ServiceName { get; set; }

        public HealthCheckEntity(DateTime timestamp, string serviceName)
        {
            PartitionKey = "HealthCheck";
            RowKey = $"{timestamp:yyyyMMddHHmmss}_{serviceName}";
            ServiceName = serviceName;
            Timestamp = timestamp;
        }

        public HealthCheckEntity() { }
    }

    public class AlertEmailEntity : TableEntity
    {
        public string Email { get; set; }

        public AlertEmailEntity(string email)
        {
            PartitionKey = "AlertEmails";
            RowKey = email.ToLowerInvariant();
            Email = email;
        }

        public AlertEmailEntity() { }
    }
}



















