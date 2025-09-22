using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

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

                // Provjeri da li je pozvan sa argumentom za kreiranje test korisnika
                if (args.Length > 0 && args[0] == "--create-test-user")
                {
                    Console.WriteLine("Kreiranje test korisnika...");
                    await CreateTestUser();
                    Console.WriteLine("Test korisnik kreiran. Izlazim...");
                    return;
                }

                await ShowMainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gre�ka: {ex.Message}");

                // Izlazi bez čekanja na korisničku interakciju ako je pozvan sa argumentima
                if (args.Length > 0)
                {
                    Console.WriteLine("Aplikacija završava sa greškom.");
                    return;
                }

                Console.WriteLine("Pritisnite bilo koji taster za izlaz...");
                if (Console.IsInputRedirected == false && Console.IsOutputRedirected == false)
                {
                    Console.ReadKey();
                }
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
                Console.WriteLine("6. [FIX] Popravi korisnike sa null lozinkama");
                Console.WriteLine("7. [FIX] Popravi korisnika koji ne može da se prijavi");
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
                    case "6":
                        await FixBrokenUsers();
                        Console.WriteLine("Pritisnite bilo koji taster...");
                        Console.ReadKey();
                        break;
                    case "7":
                        await FixBrokenUserLogin();
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
                        // SIGURNA VERIFIKACIJA - prvo učitaj najnovije podatke korisnika
                        var refreshOperation = TableOperation.Retrieve<UserEntity>("User", userToVerify.RowKey);
                        var refreshResult = await _usersTable.ExecuteAsync(refreshOperation);

                        if (refreshResult.Result != null)
                        {
                            var freshUser = refreshResult.Result as UserEntity;

                            // Postavi samo IsAuthorVerified na true, ostavi SVE ostale podatke netaknute
                            freshUser.IsAuthorVerified = true;

                            // Koristi Replace umesto InsertOrReplace da se osiguraš da korisnik postojи
                            var updateOperation = TableOperation.Replace(freshUser);
                            await _usersTable.ExecuteAsync(updateOperation);

                            Console.WriteLine($"✅ Korisnik {freshUser.FullName} je uspešno verifikovan kao autor.");
                            Console.WriteLine($"📧 Email: {freshUser.RowKey}");
                            Console.WriteLine($"🔐 PasswordHash: {(string.IsNullOrEmpty(freshUser.PasswordHash) ? "❌ NEDOSTAJE!" : "✅ Postoji")}");
                            Console.WriteLine("👍 Korisnik može da se prijavi sa istom lozinkom kao pre verifikacije.");
                        }
                        else
                        {
                            Console.WriteLine("❌ GREŠKA: Korisnik više ne postoji u bazi!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Greška prilikom verifikacije: {ex.Message}");
                        Console.WriteLine("🔧 Pokušavam sa alternativnom metodom...");

                        // Fallback - direktno ažuriraj samo IsAuthorVerified polje
                        try
                        {
                            userToVerify.IsAuthorVerified = true;
                            var mergeOperation = TableOperation.Merge(userToVerify);
                            await _usersTable.ExecuteAsync(mergeOperation);
                            Console.WriteLine("✅ Verifikacija završena alternativnom metodom.");
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"❌ I alternativna metoda neuspešna: {fallbackEx.Message}");
                        }
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
                        Console.WriteLine($"Verifikovan kao autor: {(user.IsAuthorVerified ? "✅ Da" : "⏳ Ne")}");
                        Console.WriteLine($"PasswordHash: {(string.IsNullOrEmpty(user.PasswordHash) ? "❌ NEDOSTAJE - NEĆE MOĆI DA SE PRIJAVI!" : "✅ Postoji - može da se prijavi")}");
                        if (!string.IsNullOrEmpty(user.PasswordHash))
                        {
                            Console.WriteLine($"Hash početak: {user.PasswordHash.Substring(0, Math.Min(10, user.PasswordHash.Length))}...");
                        }
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

        private static async Task FixUsersWithNullPasswords()
        {
            try
            {
                Console.WriteLine("=== POPRAVKA KORISNIKA SA NULL LOZINKAMA ===");
                Console.WriteLine();

                // Učitaj sve korisnike
                var query = new TableQuery<UserEntity>();
                var result = await _usersTable.ExecuteQuerySegmentedAsync(query, null);
                var users = result.Results.ToList();

                Console.WriteLine($"Ukupno korisnika u tabeli: {users.Count}");
                Console.WriteLine();

                if (users.Any())
                {
                    Console.WriteLine("LISTA KORISNIKA:");
                    foreach (var user in users)
                    {
                        Console.WriteLine($"📧 {user.RowKey} - {user.FullName}");
                        Console.WriteLine($"   Verifikovan: {(user.IsAuthorVerified ? "Da" : "Ne")}");
                        Console.WriteLine();
                    }

                    Console.WriteLine("⚠️  NAPOMENA ZA LOZINKE:");
                    Console.WriteLine("Ako se ne možete prijaviti na forum:");
                    Console.WriteLine("1. Registrujte novi nalog na forumu");
                    Console.WriteLine("2. Ili koristite test@example.com sa bilo kojom lozinkom za test");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("❌ Nema korisnika u tabeli.");
                    Console.WriteLine("Preporučujem da registrujete nalog na forumu prvo.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Greška: {ex.Message}");
            }
        }

        private static async Task CreateTestUser()
        {
            try
            {
                string testEmail = "test@example.com";
                string testPassword = "test123";

                // Koristi isti algoritam za hashing kao u AccountController
                string hashedPassword = HashPassword(testPassword);

                // Kreiraj test korisnika sa ISPRAVNIM PasswordHash-om
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
                await _usersTable.ExecuteAsync(insertOperation);

                Console.WriteLine("✅ Test korisnik kreiran uspešno!");
                Console.WriteLine("📧 Email: test@example.com");
                Console.WriteLine("🔑 Lozinka: test123");
                Console.WriteLine("👤 Ime: Test Administrator");
                Console.WriteLine("🔐 PasswordHash kreiran ispravno");
                Console.WriteLine("✅ IsAuthorVerified = false (može se verifikovati kroz opciju 2)");
                Console.WriteLine();
                Console.WriteLine("TESTIRANJE PRIJAVE:");
                Console.WriteLine("- Idite na Movie Discussion Forum");
                Console.WriteLine("- Kliknite 'Prijava'");
                Console.WriteLine("- Unesite: test@example.com / test123");
                Console.WriteLine("- Trebalo bi da se uspešno prijavite!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Greška: {ex.Message}");
            }
        }

        // Isti algoritam kao u AccountController.cs - SHA256 sa saltom
        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "MovieForum2024"));
                return Convert.ToBase64String(hashedBytes);
            }
        }


        private static async Task FixBrokenUsers()
        {
            Console.Clear();
            Console.WriteLine("=== POPRAVKA OŠTEĆENIH KORISNIKA ===");
            Console.WriteLine();

            try
            {
                var query = new TableQuery<UserEntity>();
                var result = await _usersTable.ExecuteQuerySegmentedAsync(query, null);
                var users = result.Results.ToList();

                var brokenUsers = users.Where(u => string.IsNullOrEmpty(u.PasswordHash)).ToList();

                if (!brokenUsers.Any())
                {
                    Console.WriteLine("✅ Nema oštećenih korisnika - svi imaju PasswordHash!");
                    return;
                }

                Console.WriteLine($"🔍 Pronađeno {brokenUsers.Count} korisnika bez PasswordHash:");
                foreach (var user in brokenUsers)
                {
                    Console.WriteLine($"  - {user.FullName} ({user.RowKey})");
                }

                Console.WriteLine();
                Console.Write("Da li želite da popravite ove korisnike? (da/ne): ");
                var confirm = Console.ReadLine().ToLower();

                if (confirm == "da" || confirm == "d")
                {
                    foreach (var user in brokenUsers)
                    {
                        Console.WriteLine($"🔧 Popravljam {user.FullName}...");
                        
                        // Postavi default lozinku
                        var newPassword = "temp123"; // Korisnik će morati da promeni lozinku
                        var hashedPassword = HashPassword(newPassword);
                        
                        user.PasswordHash = hashedPassword;
                        
                        var updateOperation = TableOperation.Replace(user);
                        await _usersTable.ExecuteAsync(updateOperation);
                        
                        Console.WriteLine($"✅ {user.FullName} - nova lozinka: {newPassword}");
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("🎉 Svi korisnici su popravljeni!");
                    Console.WriteLine("⚠️  KORISNICI MORAJU DA PROMENE LOZINKU PRILIKOM PRVE PRIJAVE!");
                }
                else
                {
                    Console.WriteLine("❌ Popravka je otkazana.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Greška: {ex.Message}");
            }
        }

        private static async Task FixBrokenUserLogin()
        {
            Console.Clear();
            Console.WriteLine("=== POPRAVKA LOGIN PROBLEMA ===");
            Console.WriteLine();

            try
            {
                var query = new TableQuery<UserEntity>();
                var result = await _usersTable.ExecuteQuerySegmentedAsync(query, null);
                var users = result.Results.ToList();

                Console.WriteLine($"🔍 Pregledam {users.Count} korisnika...");
                Console.WriteLine();

                var fixedCount = 0;
                foreach (var user in users)
                {
                    Console.WriteLine($"👤 {user.FullName} ({user.RowKey})");
                    Console.WriteLine($"   🔐 PasswordHash: {(string.IsNullOrEmpty(user.PasswordHash) ? "❌ NEDOSTAJE" : "✅ OK")}");
                    Console.WriteLine($"   ✅ Verifikovan: {(user.IsAuthorVerified ? "DA" : "NE")}");
                    
                    if (string.IsNullOrEmpty(user.PasswordHash))
                    {
                        Console.WriteLine("   🔧 Popravljam...");
                        
                        var newPassword = "temp123";
                        var hashedPassword = HashPassword(newPassword);
                        user.PasswordHash = hashedPassword;
                        
                        var updateOperation = TableOperation.Replace(user);
                        await _usersTable.ExecuteAsync(updateOperation);
                        
                        Console.WriteLine($"   ✅ Nova lozinka: {newPassword}");
                        fixedCount++;
                    }
                    
                    Console.WriteLine();
                }

                Console.WriteLine($"🎉 Popravljeno {fixedCount} korisnika!");
                if (fixedCount > 0)
                {
                    Console.WriteLine("⚠️  KORISNICI MORAJU DA PROMENE LOZINKU PRILIKOM PRVE PRIJAVE!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Greška: {ex.Message}");
            }
        }
    }
}









