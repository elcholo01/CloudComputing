# PowerShell skripta za kopiranje potrebnih DLL-ova

$sourceDir = "packages"
$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $binDebugDir = "$project\bin\Debug"
    $binReleaseDir = "$project\bin\Release"
    
    # Kreiraj direktorijume ako ne postoje
    New-Item -ItemType Directory -Path $binDebugDir -Force | Out-Null
    New-Item -ItemType Directory -Path $binReleaseDir -Force | Out-Null
    
    # Kopiraj WindowsAzure.Storage.dll
    Copy-Item "$sourceDir\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll" $binDebugDir -Force
    Copy-Item "$sourceDir\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll" $binReleaseDir -Force
    
    # Kopiraj Newtonsoft.Json.dll
    Copy-Item "$sourceDir\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" $binDebugDir -Force
    Copy-Item "$sourceDir\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" $binReleaseDir -Force
    
    # Kopiraj Microsoft.WindowsAzure.Configuration.dll
    Copy-Item "$sourceDir\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll" $binDebugDir -Force
    Copy-Item "$sourceDir\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll" $binReleaseDir -Force
    
    Write-Host "Kopirani DLL-ovi u $project"
}

Write-Host "Svi DLL-ovi su kopirani!"
