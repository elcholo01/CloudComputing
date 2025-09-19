using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AdminToolsConsoleApp;

namespace AdminToolsConsoleApp
{
    class Program
    {
        private static CloudTable _usersTable;
        private static CloudTable _alertEmailsTable;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Movie Discussion Forum - Admin Tools ===");
            Console.WriteLine();

            try
            {
                InitializeStorage();
                await ShowMainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gre�ka: {ex.Message}");
                Console.WriteLine("Pritisnite bilo koji taster za izlaz...");
                Console.ReadKey();
            }
        }

        private static void InitializeStorage()
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataConnectionString"].ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            
            _usersTable = tableClient.GetTableReference("Users");
            _alertEmailsTable = tableClient.GetTableReference("AlertEmails");
            
            _usersTable.CreateIfNotExists();
            _alertEmailsTable.CreateIfNotExists();
        }

        private static async Task ShowMainMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== GLAVNI MENI ===");
                Console.WriteLine("1. Upravljanje email adresama za upozorenja");
                Console.WriteLine("2. Verifikacija korisnika kao autora");
                Console.WriteLine("3. Pregled svih korisnika");
                Console.WriteLine("4. Pregled email adresa za upozorenja");
                Console.WriteLine("5. [TEST] Kreiraj test korisnika"); 
                Console.WriteLine("0. Izlaz");
                Console.WriteLine();
                Console.Write("Izaberite opciju: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await ManageAlertEmails();
                        break;
                    case "2":
                        await VerifyAuthor();
                        break;
                    case "3":
                        await ListAllUsers();
                        break;
                    case "4":
                        await ListAlertEmails();
                        break;
                    case "5":
                        await CreateTestUser();
                        Console.WriteLine("Pritisnite bilo koji taster...");
                        Console.ReadKey();
                        break;
                    case "0":
                        Console.WriteLine("Dovidenja!");
                        return;
                    default:
                        Console.WriteLine("Neispravan izbor. Pritisnite bilo koji taster...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static async Task ManageAlertEmails()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== UPRAVLJANJE EMAIL ADRESAMA ZA UPOZORENJA ===");
                Console.WriteLine("1. Dodaj novu email adresu");
                Console.WriteLine("2. Ukloni email adresu");
                Console.WriteLine("3. Povratak na glavni meni");
                Console.WriteLine();
                Console.Write("Izaberite opciju: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await AddAlertEmail();
                        break;
                    case "2":
                        await RemoveAlertEmail();
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Neispravan izbor. Pritisnite bilo koji taster...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static async Task AddAlertEmail()
        {
            Console.Write("Unesite email adresu: ");
            var email = Console.ReadLine().Trim();

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                Console.WriteLine("Neispravan format email adrese.");
                Console.WriteLine("Pritisnite bilo koji taster...");
                Console.ReadKey();
                return;
            }

            try
            {
                var alertEmail = new AlertEmailEntity(email);
                var insertOperation = TableOperation.InsertOrReplace(alertEmail);
                await _alertEmailsTable.ExecuteAsync(insertOperation);

                Console.WriteLine($"Email adresa {email} je uspe�no dodata.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gre�ka prilikom dodavanja email adrese: {ex.Message}");
            }

            Console.WriteLine("Pritisnite bilo koji taster...");
            Console.ReadKey();
        }

        private static async Task RemoveAlertEmail()
        {
            var emails = await GetAlertEmails();
            
            if (!emails.Any())
            {
                Console.WriteLine("Nema konfigurisanih email adresa.");
                Console.WriteLine("Pritisnite bilo koji taster...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Dostupne email adrese:");
            for (int i = 0; i < emails.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {emails[i]}");
            }

            Console.Write("Izaberite broj email adrese za uklanjanje: ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= emails.Count)
            {
                var emailToRemove = emails[choice - 1];
                
                try
                {
                    var alertEmail = new AlertEmailEntity(emailToRemove);
                    var deleteOperation = TableOperation.Delete(alertEmail);
                    await _alertEmailsTable.ExecuteAsync(deleteOperation);

                    Console.WriteLine($"Email adresa {emailToRemove} je uspe�no uklonjena.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gre�ka prilikom uklanjanja email adrese: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Neispravan izbor.");
            }

            Console.WriteLine("Pritisnite bilo koji taster...");
            Console.ReadKey();
        }

        private static async Task<List<string>> GetAlertEmails()
        {
            var query = new TableQuery<AlertEmailEntity>();
            var result = await _alertEmailsTable.ExecuteQuerySegmentedAsync(query, null);
            return result.Results.Select(e => e.Email).ToList();
        }

        private static async Task VerifyAuthor()
        {
            Console.WriteLine("Učitavam korisnike...");
            var users = await GetUnverifiedUsers();
            
            Console.WriteLine($"Pronađeno {users.Count} korisnika koji čekaju verifikaciju.");
            
            if (!users.Any())
            {
                Console.WriteLine("Nema korisnika koji čekaju verifikaciju.");
                Console.WriteLine();
                
                // Prikaži ukupan broj korisnika za debug
                try
                {
                    var allQuery = new TableQuery<UserEntity>();
                    var allResult = await _usersTable.ExecuteQuerySegmentedAsync(allQuery, null);
                    var allUsers = allResult.Results.ToList();
                    
                    Console.WriteLine($"DEBUG: Ukupno korisnika u bazi: {allUsers.Count}");
                    foreach (var user in allUsers)
                    {
                        Console.WriteLine($"  - {user.FullName} ({user.RowKey}) - Verifikovan: {user.IsAuthorVerified}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška pri debug pregledu: {ex.Message}");
                }
                
                Console.WriteLine();
                Console.WriteLine("Pritisnite bilo koji taster...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Korisnici koji cekaju verifikaciju:");
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                Console.WriteLine($"{i + 1}. {user.FullName} ({user.RowKey}) - {user.Country}, {user.City}");
            }

            Console.Write("Izaberite broj korisnika za verifikaciju: ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= users.Count)
            {
                var userToVerify = users[choice - 1];
                
                Console.Write($"Da li ste sigurni da �elite da verifikujete {userToVerify.FullName} kao autora? (da/ne): ");
                var confirm = Console.ReadLine().ToLower();

                if (confirm == "da" || confirm == "d")
                {
                    try
                    {
                        userToVerify.IsAuthorVerified = true;
                        var updateOperation = TableOperation.InsertOrReplace(userToVerify);
                        await _usersTable.ExecuteAsync(updateOperation);

                        Console.WriteLine($"Korisnik {userToVerify.FullName} je uspe�no verifikovan kao autor.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Gre�ka prilikom verifikacije: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Verifikacija je otkazana.");
                }
            }
            else
            {
                Console.WriteLine("Neispravan izbor.");
            }

            Console.WriteLine("Pritisnite bilo koji taster...");
            Console.ReadKey();
        }

        private static async Task<List<UserEntity>> GetUnverifiedUsers()
        {
            try
            {
                // Učitaj sve korisnike jer boolean filter može da pravi probleme
                var query = new TableQuery<UserEntity>();
                var result = await _usersTable.ExecuteQuerySegmentedAsync(query, null);
                
                // Filtriraj lokalno
                return result.Results.Where(u => !u.IsAuthorVerified).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri učitavanju korisnika: {ex.Message}");
                return new List<UserEntity>();
            }
        }

        private static async Task ListAllUsers()
        {
            Console.Clear();
            Console.WriteLine("=== PREGLED SVIH KORISNIKA ===");
            Console.WriteLine();

            try
            {
                var query = new TableQuery<UserEntity>();
                var result = await _usersTable.ExecuteQuerySegmentedAsync(query, null);
                var users = result.Results.ToList();

                if (!users.Any())
                {
                    Console.WriteLine("Nema registrovanih korisnika.");
                }
                else
                {
                    Console.WriteLine($"Ukupno korisnika: {users.Count}");
                    Console.WriteLine();

                    foreach (var user in users.OrderBy(u => u.FullName))
                    {
                        Console.WriteLine($"Ime: {user.FullName}");
                        Console.WriteLine($"Email: {user.RowKey}");
                        Console.WriteLine($"Pol: {user.Gender}");
                        Console.WriteLine($"Lokacija: {user.Country}, {user.City}");
                        Console.WriteLine($"Verifikovan kao autor: {(user.IsAuthorVerified ? "Da" : "Ne")}");
                        Console.WriteLine(new string('-', 50));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gre�ka prilikom ucitavanja korisnika: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Pritisnite bilo koji taster za povratak...");
            Console.ReadKey();
        }

        private static async Task ListAlertEmails()
        {
            Console.Clear();
            Console.WriteLine("=== PREGLED EMAIL ADRESA ZA UPOZORENJA ===");
            Console.WriteLine();

            try
            {
                var emails = await GetAlertEmails();

                if (!emails.Any())
                {
                    Console.WriteLine("Nema konfigurisanih email adresa za upozorenja.");
                }
                else
                {
                    Console.WriteLine($"Ukupno email adresa: {emails.Count}");
                    Console.WriteLine();

                    for (int i = 0; i < emails.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {emails[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gre�ka prilikom ucitavanja email adresa: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Pritisnite bilo koji taster za povratak...");
            Console.ReadKey();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static async Task CreateTestUser()
        {
            try
            {
                // Kreiraj test korisnika
                var testUser = new Common.UserEntity("test@example.com")
                {
                    FullName = "Test Korisnik",
                    Gender = "Muški", 
                    Country = "Srbija",
                    City = "Beograd",
                    Address = "Test adresa 123",
                    PasswordHash = "test-hash", // Za testiranje
                    PhotoUrl = "",
                    IsAuthorVerified = false // NEPROVERENI!
                };

                var insertOperation = TableOperation.InsertOrReplace(testUser);
                await _usersTable.ExecuteAsync(insertOperation);

                Console.WriteLine("✅ Test korisnik kreiran: test@example.com (IsAuthorVerified = false)");
                Console.WriteLine("Sada možete testirati verifikaciju u opciji 2!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Greška: {ex.Message}");
            }
        }
    }
}









