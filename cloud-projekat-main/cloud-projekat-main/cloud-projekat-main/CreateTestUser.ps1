# PowerShell script to create test user directly in Azure Storage Emulator
Write-Host "=== Kreiranje test korisnika za Movie Discussion Forum ===" -ForegroundColor Green

# Load necessary assemblies
Add-Type -Path "C:\Users\Darie\Desktop\projektiDavid\CloudComputing\cloud-projekat-main\cloud-projekat-main\cloud-projekat-main\packages\WindowsAzure.Storage.9.3.3\lib\net40\Microsoft.WindowsAzure.Storage.dll"
Add-Type -Path "C:\Users\Darie\Desktop\projektiDavid\CloudComputing\cloud-projekat-main\cloud-projekat-main\cloud-projekat-main\Common\bin\Debug\Common.dll"

try {
    # Initialize Azure Storage connection
    $connectionString = "UseDevelopmentStorage=true"
    $storageAccount = [Microsoft.WindowsAzure.Storage.CloudStorageAccount]::Parse($connectionString)
    $tableClient = $storageAccount.CreateCloudTableClient()
    $usersTable = $tableClient.GetTableReference("Users")

    # Create table if it doesn't exist
    $usersTable.CreateIfNotExists()

    # Test user credentials
    $testEmail = "test@example.com"
    $testPassword = "test123"

    # Hash password using SHA256 with salt (same as forum)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hashedBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($testPassword + "MovieForum2024"))
    $hashedPassword = [Convert]::ToBase64String($hashedBytes)
    $sha256.Dispose()

    # Create UserEntity
    $testUser = New-Object Common.UserEntity($testEmail)
    $testUser.FullName = "Test Administrator"
    $testUser.Gender = "Muski"
    $testUser.Country = "Srbija"
    $testUser.City = "Beograd"
    $testUser.Address = "Test adresa 123"
    $testUser.PasswordHash = $hashedPassword
    $testUser.PhotoUrl = ""
    $testUser.IsAuthorVerified = $false

    # Insert or replace user in table
    $insertOperation = [Microsoft.WindowsAzure.Storage.Table.TableOperation]::InsertOrReplace($testUser)
    $result = $usersTable.Execute($insertOperation)

    if ($result.HttpStatusCode -ge 200 -and $result.HttpStatusCode -lt 300) {
        Write-Host "✅ Test korisnik uspešno kreiran!" -ForegroundColor Green
        Write-Host "📧 Email: test@example.com" -ForegroundColor Cyan
        Write-Host "🔑 Lozinka: test123" -ForegroundColor Cyan
        Write-Host "👤 Ime: Test Administrator" -ForegroundColor Cyan
        Write-Host "🔐 PasswordHash kreiran uspešno" -ForegroundColor Green
        Write-Host ""
        Write-Host "TESTIRANJE PRIJAVE:" -ForegroundColor Yellow
        Write-Host "1. Pokrenite Movie Discussion Forum" -ForegroundColor White
        Write-Host "2. Kliknite na 'Prijava'" -ForegroundColor White
        Write-Host "3. Unesite: test@example.com / test123" -ForegroundColor White
        Write-Host "4. Trebalo bi da se uspešno prijavite!" -ForegroundColor White
    } else {
        Write-Host "❌ Greška: HTTP status $($result.HttpStatusCode)" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Greška: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Pritisnite bilo koji taster za nastavak..."
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null