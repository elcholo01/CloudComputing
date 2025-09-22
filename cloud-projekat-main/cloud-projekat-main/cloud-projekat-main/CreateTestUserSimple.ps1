# Simple PowerShell script to create test user
Write-Host "Creating test user for Movie Discussion Forum..." -ForegroundColor Green

try {
    # Load Azure Storage assembly
    $assemblyPath = "C:\Users\Darie\Desktop\projektiDavid\CloudComputing\cloud-projekat-main\cloud-projekat-main\cloud-projekat-main\AdminToolsConsoleApp\bin\Debug\Microsoft.WindowsAzure.Storage.dll"
    Add-Type -Path $assemblyPath

    # Initialize connection
    $connectionString = "UseDevelopmentStorage=true"
    $storageAccount = [Microsoft.WindowsAzure.Storage.CloudStorageAccount]::Parse($connectionString)
    $tableClient = $storageAccount.CreateCloudTableClient()
    $usersTable = $tableClient.GetTableReference("Users")
    $usersTable.CreateIfNotExists()

    # Hash password
    $testPassword = "test123"
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hashedBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($testPassword + "MovieForum2024"))
    $hashedPassword = [Convert]::ToBase64String($hashedBytes)
    $sha256.Dispose()

    Write-Host "Password hash created: $($hashedPassword.Substring(0,20))..." -ForegroundColor Cyan
    Write-Host "Test user credentials: test@example.com / test123" -ForegroundColor Yellow

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Done!" -ForegroundColor Green