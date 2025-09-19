using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService
{
    public class WorkerRole 
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private CloudQueue _queue;
        private CloudTable _subsTable;
        private CloudTable _logTable;
        private CloudTable _commentsTable;
        private IEmailSender _email;

        public void Run()
        {
            Trace.TraceInformation("NotificationService is running");

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

            // Queue
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference("notifications");
            _queue.CreateIfNotExists();

            // Tables
            var tableClient = storageAccount.CreateCloudTableClient();
            _subsTable = tableClient.GetTableReference("FollowTable");
            _logTable = tableClient.GetTableReference("NotificationLog");
            _commentsTable = tableClient.GetTableReference("Comments");
            
            _subsTable.CreateIfNotExists();
            _logTable.CreateIfNotExists();
            _commentsTable.CreateIfNotExists();

            // Email sender (SMTP verzija) - čita konfiguraciju iz app.config
            var smtpHost = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            var smtpUser = System.Configuration.ConfigurationManager.AppSettings["SmtpUser"] ?? "your-email@gmail.com";
            var smtpPass = System.Configuration.ConfigurationManager.AppSettings["SmtpPass"] ?? "your-app-password";
            var fromEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmail"] ?? "your-email@gmail.com";

            _email = new SMTPEmailSender(smtpHost, smtpPort, smtpUser, smtpPass, fromEmail);

            Trace.TraceInformation("NotificationService has been started");

            return true;
        }

        public void OnStop()
        {
            Trace.TraceInformation("NotificationService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            Trace.TraceInformation("NotificationService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await _queue.GetMessageAsync();
                if (msg != null)
                {
                    try
                    {
                        // 1. Deserialize message
                        var payload = JsonConvert.DeserializeObject<QueueMessagePayload>(msg.AsString);

                        // 2. Fetch all subscribers
                        var query = new TableQuery<FollowEntity>().Where(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, payload.DiscussionId));

                        var followers = await _subsTable.ExecuteQuerySegmentedAsync(query, null);

                        // 2b. Izvuci sve email adrese
                        var recipientList = followers.Results
                            .Select(f => f.UserEmail)
                            .Where(e => !string.IsNullOrWhiteSpace(e))
                            .Distinct()
                            .ToList();

                        // 3. Uzmi komentar
                        var retrieve = TableOperation.Retrieve<CommentEntity>(payload.DiscussionId, payload.CommentId);
                        var resultComment = await _commentsTable.ExecuteAsync(retrieve);
                        var commentEntity = resultComment.Result as CommentEntity;

                        if (commentEntity != null && recipientList.Any())
                        {
                            string body = $"Novi komentar na diskusiji:\n\n{commentEntity.Text}\n\nAutor: {commentEntity.AuthorEmail}\nDatum: {commentEntity.CreatedUtc:dd.MM.yyyy HH:mm}";

                            // 4. Send emails
                            await _email.SendAsync(recipientList, "Novi komentar na diskusiji", body);

                            // 5. Log notification
                            var sentCount = recipientList.Count;
                            var log = new NotificationLogEntity(payload.CommentId, DateTime.UtcNow, sentCount);
                            var insert = TableOperation.InsertOrReplace(log);
                            await _logTable.ExecuteAsync(insert);

                            Trace.TraceInformation($"Processed comment {payload.CommentId}, sent {sentCount} emails");
                        }
                        else
                        {
                            Trace.TraceWarning($"Comment {payload.CommentId} not found or no recipients");
                        }

                        // 6. Remove message from queue
                        await _queue.DeleteMessageAsync(msg);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Greška prilikom obrade: " + ex.Message);
                        
                        // Ukloni poruku iz reda čak i ako je došlo do greške
                        // da ne bi blokirala red
                        try
                        {
                            await _queue.DeleteMessageAsync(msg);
                        }
                        catch (Exception deleteEx)
                        {
                            Trace.TraceError("Greška prilikom brisanja poruke iz reda: " + deleteEx.Message);
                        }
                    }
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        static void Main(string[] args)
        {
            var workerRole = new WorkerRole();
            workerRole.Run();
        }
    }
}



















