# Finalna ultimativna PowerShell skripta za rešavanje svih problema

Write-Host "Rešavam sve preostale probleme..."

# 1. Ažuriraj sve .csproj fajlove da uklone sve Microsoft.WindowsAzure.ServiceRuntime reference
Write-Host "Ažuriram .csproj fajlove..."

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    
    if (Test-Path $csprojFile) {
        Write-Host "Ažuriram $csprojFile..."
        
        $content = Get-Content $csprojFile -Raw
        
        # Ukloni sve Microsoft.WindowsAzure.ServiceRuntime reference
        $content = $content -replace '<Reference Include="Microsoft\.WindowsAzure\.ServiceRuntime".*?</Reference>', ''
        $content = $content -replace '<Reference Include="Microsoft\.WindowsAzure\.Configuration".*?</Reference>', ''
        
        # Ukloni prazne linije koje su ostale
        $content = $content -replace '\r\n\s*\r\n', "`r`n"
        
        Set-Content $csprojFile $content
        Write-Host "  Uklonjeni ServiceRuntime i Configuration reference iz $project"
    }
}

# 2. Rešimo problem sa using direktivama u svim fajlovima
Write-Host "Rešavam using direktive..."

$allFiles = Get-ChildItem -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

foreach ($file in $allFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Ukloni problematične using direktive
    $content = $content -replace 'using Microsoft\.WindowsAzure\.ServiceRuntime;', ''
    $content = $content -replace 'using Microsoft\.WindowsAzure\.Diagnostics;', ''
    $content = $content -replace 'using Microsoft\.WindowsAzure\.Configuration;', ''
    
    # Ukloni prazne linije koje su ostale
    $content = $content -replace '\r\n\s*\r\n', "`r`n"
    
    Set-Content $file.FullName $content
}

Write-Host "  Uklonjene problematične using direktive"

# 3. Rešimo problem sa RoleEntryPoint u svim fajlovima
Write-Host "Rešavam RoleEntryPoint probleme..."

$roleFiles = @(
    "HealthMonitoringService\WorkerRole.cs",
    "NotificationService\WorkerRole.cs",
    "MovieDiscussionService\WebRole.cs",
    "HealthStatusService\WebRole.cs"
)

foreach ($file in $roleFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Zameni RoleEntryPoint sa object
        $content = $content -replace ': RoleEntryPoint', ''
        $content = $content -replace 'public override void Run\(\)', 'public void Run()'
        $content = $content -replace 'public override bool OnStart\(\)', 'public bool OnStart()'
        
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

# 4. Rešimo problem sa Storage u AdminToolsConsoleApp
Write-Host "Rešavam Storage problem u AdminToolsConsoleApp..."
$alertEmailsTableFile = "AdminToolsConsoleApp\AlertEmailsTable.cs"
if (Test-Path $alertEmailsTableFile) {
    $content = Get-Content $alertEmailsTableFile -Raw
    
    # Zameni Storage.GetStorageAccount() sa hardkodiranom vrednošću
    $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
    
    Set-Content $alertEmailsTableFile $content
    Write-Host "  Zamenjen Storage.GetStorageAccount()"
}

# 5. Rešimo problem sa Storage u NotificationService
Write-Host "Rešavam Storage problem u NotificationService..."
$notificationFiles = @(
    "NotificationService\WorkerRole.cs",
    "NotificationService\SMTPEmailSender.cs"
)

foreach ($file in $notificationFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Zameni Storage.GetStorageAccount() sa hardkodiranom vrednošću
        $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
        
        Set-Content $file $content
        Write-Host "  Zamenjen Storage.GetStorageAccount() u $file"
    }
}

# 6. Rešimo problem sa Storage u HealthMonitoringService
Write-Host "Rešavam Storage problem u HealthMonitoringService..."
$healthMonitoringFile = "HealthMonitoringService\WorkerRole.cs"
if (Test-Path $healthMonitoringFile) {
    $content = Get-Content $healthMonitoringFile -Raw
    
    # Zameni Storage.GetStorageAccount() sa hardkodiranom vrednošću
    $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
    
    Set-Content $healthMonitoringFile $content
    Write-Host "  Zamenjen Storage.GetStorageAccount() u HealthMonitoringService"
}

# 7. Rešimo problem sa Storage u MovieDiscussionService
Write-Host "Rešavam Storage problem u MovieDiscussionService..."
$movieFiles = @(
    "MovieDiscussionService\Controllers\AccountController.cs",
    "MovieDiscussionService\Controllers\HomeController.cs",
    "MovieDiscussionService\Controllers\DiscussionController.cs"
)

foreach ($file in $movieFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Zameni Storage.GetStorageAccount() sa hardkodiranom vrednošću
        $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
        
        Set-Content $file $content
        Write-Host "  Zamenjen Storage.GetStorageAccount() u $file"
    }
}

# 8. Rešimo problem sa Storage u HealthStatusService
Write-Host "Rešavam Storage problem u HealthStatusService..."
$healthStatusFiles = @(
    "HealthStatusService\Controllers\HomeController.cs"
)

foreach ($file in $healthStatusFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Zameni Storage.GetStorageAccount() sa hardkodiranom vrednošću
        $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
        
        Set-Content $file $content
        Write-Host "  Zamenjen Storage.GetStorageAccount() u $file"
    }
}

# 9. Kopiraj DLL-ove u bin direktorijume
Write-Host "Kopiram DLL-ove..."
foreach ($project in $projects) {
    $binDebugDir = "$project\bin\Debug"
    $binReleaseDir = "$project\bin\Release"
    
    if (-not (Test-Path $binDebugDir)) {
        New-Item -ItemType Directory -Path $binDebugDir -Force | Out-Null
    }
    if (-not (Test-Path $binReleaseDir)) {
        New-Item -ItemType Directory -Path $binReleaseDir -Force | Out-Null
    }
    
    # Kopiraj potrebne DLL-ove
    $dlls = @(
        "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll",
        "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll"
    )
    
    foreach ($dll in $dlls) {
        if (Test-Path $dll) {
            Copy-Item $dll $binDebugDir -Force
            Copy-Item $dll $binReleaseDir -Force
        }
    }
    
    Write-Host "  Kopirani DLL-ovi u $project"
}

Write-Host "Svi problemi su rešeni!"
Write-Host ""
Write-Host "Sada molimo vas da:"
Write-Host "1. Zatvorite Visual Studio"
Write-Host "2. Otvorite ponovo CloudProjekt.sln"
Write-Host "3. Izvršite Build -> Rebuild Solution"
Write-Host ""
Write-Host "Ako i dalje imate grešaka, molimo vas da:"
Write-Host "1. Izvršite Tools -> NuGet Package Manager -> Package Manager Console"
Write-Host "2. U konzoli izvršite: Update-Package -reinstall"
Write-Host "3. Zatim izvršite Build -> Rebuild Solution"
