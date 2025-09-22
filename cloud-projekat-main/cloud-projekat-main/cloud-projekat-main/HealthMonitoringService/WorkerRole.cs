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
using Common;

namespace HealthMonitoringService
{
    public class WorkerRole 
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private CloudTable _healthCheckTable;
        private CloudTable _alertEmailsTable;
        private HttpClient _httpClient;
        private IEmailSender _emailSender;

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
            try
            {
                Console.WriteLine("1. Postavljanje TLS i konekcija...");
                // Use TLS 1.2 for Service Bus connections
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;

                Console.WriteLine("2. Povezivanje sa Azure Storage...");
                var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");

                Console.WriteLine("3. Kreiranje tabela...");
                // Tables
                var tableClient = storageAccount.CreateCloudTableClient();
                _healthCheckTable = tableClient.GetTableReference("HealthCheck");
                _alertEmailsTable = tableClient.GetTableReference("AlertEmails");

                Console.WriteLine("4. Kreiranje tabela ako ne postoje...");
                _healthCheckTable.CreateIfNotExists();
                _alertEmailsTable.CreateIfNotExists();

                Console.WriteLine("5. Inicijalizacija HTTP klijenta...");
                // HTTP client for health checks
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(10);

                Console.WriteLine("6. Konfiguracija email servisa...");
                // Email sender za alert-ove
                var smtpHost = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                var smtpUser = System.Configuration.ConfigurationManager.AppSettings["SmtpUser"] ?? "your-email@gmail.com";
                var smtpPass = System.Configuration.ConfigurationManager.AppSettings["SmtpPass"] ?? "your-app-password";
                var fromEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmail"] ?? "your-email@gmail.com";

                _emailSender = new SMTPEmailSender(smtpHost, smtpPort, smtpUser, smtpPass, fromEmail);

                Console.WriteLine("7. HealthMonitoringService uspešno inicijalizovan!");
                Trace.TraceInformation("HealthMonitoringService has been started");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GREŠKA u OnStart(): {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Trace.TraceError($"Greška u OnStart(): {ex}");
                return false;
            }
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
                    var subject = $"ALERT: {serviceName} nije dostupan";
                    var body = $"Servis: {serviceName}\n" +
                              $"Vreme: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} UTC\n" +
                              $"Poruka: {message}\n\n" +
                              $"Molimo proverite status servisa.";

                    var recipients = alertEmails.Select(e => e.Email).ToList();
                    
                    try
                    {
                        await _emailSender.SendAsync(recipients, subject, body);
                        Trace.TraceInformation($"Alert email poslat na {recipients.Count} adresa za servis {serviceName}");
                    }
                    catch (Exception emailEx)
                    {
                        Trace.TraceError($"Greška prilikom slanja email-a: {emailEx.Message}");
                        // Fallback - samo loguj
                        Trace.TraceWarning($"ALERT: {message}");
                        Trace.TraceWarning($"Trebalo bi poslati email na: {string.Join(", ", recipients)}");
                    }
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

            Console.WriteLine("Pokretanje HealthMonitoringService...");

            if (workerRole.OnStart())
            {
                Console.WriteLine("HealthMonitoringService uspešno pokrenuti. Pritisnite bilo koji taster za zaustavljanje...");

                // Pokreni u background thread da možemo da kontrolišemo
                var runTask = Task.Run(() => workerRole.Run());

                // Čekaj da korisnik pritisne taster
                Console.ReadKey();

                Console.WriteLine("Zaustavljanje servisa...");
                workerRole.OnStop();
            }
            else
            {
                Console.WriteLine("Neuspešno pokretanje HealthMonitoringService!");
                Console.WriteLine("Pritisnite bilo koji taster za izlaz...");
                Console.ReadKey();
            }
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

    public class SMTPEmailSender : IEmailSender
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;

        public SMTPEmailSender(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromEmail)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUser = smtpUser;
            _smtpPass = smtpPass;
            _fromEmail = fromEmail;
        }

        public async Task SendAsync(IEnumerable<string> recipients, string subject, string body)
        {
            if (recipients == null || !recipients.Any())
            {
                throw new ArgumentException("Lista primalaca ne može biti prazna.");
            }

            try
            {
                using (var client = new System.Net.Mail.SmtpClient(_smtpHost, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPass);
                    client.EnableSsl = true;
                    client.Timeout = 30000; // 30 sekundi timeout

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_fromEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    };

                    // Dodaj sve primaoce
                    foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Greška prilikom slanja emaila: {ex.Message}", ex);
            }
        }

        public async Task SendAsync(string recipient, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(recipient))
            {
                throw new ArgumentException("Primalac ne može biti prazan.");
            }

            await SendAsync(new[] { recipient }, subject, body);
        }
    }
}



















