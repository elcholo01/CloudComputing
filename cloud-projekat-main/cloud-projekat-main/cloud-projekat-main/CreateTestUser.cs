using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Common;

class CreateTestUser
{
    static async Task Main()
    {
        try
        {
            Console.WriteLine("=== Kreiranje test korisnika ===");

            // Inicijalizacija Azure Storage
            var connectionString = "UseDevelopmentStorage=true";
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var usersTable = tableClient.GetTableReference("Users");

            await usersTable.CreateIfNotExistsAsync();

            string testEmail = "test@example.com";
            string testPassword = "test123";

            // Hash password using same algorithm as forum
            string hashedPassword = HashPassword(testPassword);

            // Create test user
            var testUser = new UserEntity(testEmail)
            {
                FullName = "Test Administrator",
                Gender = "Muski",
                Country = "Srbija",
                City = "Beograd",
                Address = "Test adresa 123",
                PasswordHash = hashedPassword,
                PhotoUrl = "",
                IsAuthorVerified = false
            };

            var insertOperation = TableOperation.InsertOrReplace(testUser);
            var result = await usersTable.ExecuteAsync(insertOperation);

            if (result.HttpStatusCode >= 200 && result.HttpStatusCode < 300)
            {
                Console.WriteLine("✅ Test korisnik uspešno kreiran!");
                Console.WriteLine("📧 Email: test@example.com");
                Console.WriteLine("🔑 Lozinka: test123");
                Console.WriteLine("👤 Ime: Test Administrator");
                Console.WriteLine("🔐 PasswordHash: " + hashedPassword.Substring(0, 20) + "...");
                Console.WriteLine();
                Console.WriteLine("TESTIRANJE PRIJAVE:");
                Console.WriteLine("1. Pokrenite Movie Discussion Forum");
                Console.WriteLine("2. Kliknite na 'Prijava'");
                Console.WriteLine("3. Unesite credentials: test@example.com / test123");
                Console.WriteLine("4. Trebalo bi da se uspešno prijavite!");
            }
            else
            {
                Console.WriteLine($"❌ Greška: HTTP status {result.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Greška: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    // Same hashing algorithm as AccountController.cs
    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "MovieForum2024"));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}