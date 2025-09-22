using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;
using Common;

class CreateTestHealthData
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏗️ Kreiranje test podataka za Health Check tabelu...");

        try
        {
            // Konektuj se na Azure Storage Emulator
            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var tableClient = storageAccount.CreateCloudTableClient();
            var healthCheckTable = tableClient.GetTableReference("HealthCheck");

            // Kreiraj tabelu ako ne postoji
            await healthCheckTable.CreateIfNotExistsAsync();
            Console.WriteLine("✅ HealthCheck tabela kreirana/pronađena");

            // Kreiraj test podatke za poslednja 2 sata
            var now = DateTime.UtcNow;
            var services = new[] { "MovieDiscussionService", "NotificationService", "HealthStatusService" };

            Console.WriteLine("📝 Unosim test podatke...");

            var insertCount = 0;
            var batch = new TableBatchOperation();

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
                await healthCheckTable.ExecuteAsync(insertOperation);

                insertCount++;

                if (insertCount % 10 == 0)
                {
                    Console.WriteLine($"📊 Uneseno {insertCount} zapisa...");
                }
            }

            Console.WriteLine($"✅ Uspešno uneseno {insertCount} test zapisa za health monitoring!");
            Console.WriteLine("📋 Raspored:");
            Console.WriteLine($"   - MovieDiscussionService: ~{insertCount / 3} zapisa");
            Console.WriteLine($"   - NotificationService: ~{insertCount / 3} zapisa");
            Console.WriteLine($"   - HealthStatusService: ~{insertCount / 3} zapisa");
            Console.WriteLine($"⏰ Vremenski opseg: {now.AddHours(-4):yyyy-MM-dd HH:mm} do {now:yyyy-MM-dd HH:mm} UTC");

            // Proveri da li su podaci uneseni
            var query = new TableQuery<HealthCheckEntity>().Take(10);
            var result = await healthCheckTable.ExecuteQuerySegmentedAsync(query, null);
            Console.WriteLine($"🔍 Verifikacija: Pronađeno {result.Results.Count} zapisa u tabeli");

            Console.WriteLine("\n🎯 Sada možeš testirati kompletan dashboard!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GREŠKA: {ex.Message}");
            Console.WriteLine($"📍 Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPritisnite bilo koji taster za izlaz...");
        Console.ReadKey();
    }
}