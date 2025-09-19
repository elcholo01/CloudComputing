# Finalna kompletna PowerShell skripta za rešavanje svih problema

Write-Host "Rešavam sve preostale probleme..."

# 1. Rešimo problem sa CloudConfigurationManager u Common/Storage.cs
Write-Host "Rešavam CloudConfigurationManager problem..."
$storageFile = "Common\Storage.cs"
if (Test-Path $storageFile) {
    $content = Get-Content $storageFile -Raw
    
    # Zameni CloudConfigurationManager.GetSetting sa ConfigurationManager.AppSettings
    $content = $content -replace 'CloudConfigurationManager\.GetSetting\("DataConnectionString"\)', 'ConfigurationManager.AppSettings["DataConnectionString"]'
    
    Set-Content $storageFile $content
    Write-Host "  Zamenjen CloudConfigurationManager sa ConfigurationManager"
}

# 2. Rešimo problem sa using direktivama u HealthStatusService/Controllers/HomeController.cs
Write-Host "Rešavam using direktive u HomeController..."
$homeControllerFile = "HealthStatusService\Controllers\HomeController.cs"
if (Test-Path $homeControllerFile) {
    $content = Get-Content $homeControllerFile -Raw
    
    # Dodaj potrebne using direktive
    $newUsingDirectives = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

"@
    
    # Zameni postojeće using direktive
    $content = $content -replace 'using System;.*?using System\.Web;', $newUsingDirectives.Trim()
    
    Set-Content $homeControllerFile $content
    Write-Host "  Ažuriran HomeController"
}

# 3. Rešimo problem sa using direktivama u HealthStatusService/WebRole.cs
Write-Host "Rešavam using direktive u HealthStatusService WebRole..."
$webRoleFile = "HealthStatusService\WebRole.cs"
if (Test-Path $webRoleFile) {
    $content = Get-Content $webRoleFile -Raw
    
    # Dodaj potrebne using direktive
    $newUsingDirectives = @"
using System;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;

"@
    
    # Zameni postojeće using direktive
    $content = $content -replace 'using System;.*?using System\.Web;', $newUsingDirectives.Trim()
    
    Set-Content $webRoleFile $content
    Write-Host "  Ažuriran HealthStatusService WebRole"
}

# 4. Rešimo problem sa using direktivama u MovieDiscussionService/WebRole.cs
Write-Host "Rešavam using direktive u MovieDiscussionService WebRole..."
$movieWebRoleFile = "MovieDiscussionService\WebRole.cs"
if (Test-Path $movieWebRoleFile) {
    $content = Get-Content $movieWebRoleFile -Raw
    
    # Dodaj potrebne using direktive
    $newUsingDirectives = @"
using System;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;

"@
    
    # Zameni postojeće using direktive
    $content = $content -replace 'using System;.*?using System\.Web;', $newUsingDirectives.Trim()
    
    Set-Content $movieWebRoleFile $content
    Write-Host "  Ažuriran MovieDiscussionService WebRole"
}

# 5. Rešimo problem sa using direktivama u WorkerRole fajlovima
Write-Host "Rešavam using direktive u WorkerRole fajlovima..."
$workerRoleFiles = @(
    "HealthMonitoringService\WorkerRole.cs",
    "NotificationService\WorkerRole.cs"
)

foreach ($file in $workerRoleFiles) {
    if (Test-Path $file) {
        Write-Host "Ažuriram $file..."
        
        $content = Get-Content $file -Raw
        
        # Dodaj potrebne using direktive
        $newUsingDirectives = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.ServiceRuntime;

"@
        
        # Zameni postojeće using direktive
        $content = $content -replace 'using System;.*?using System\.Threading;', $newUsingDirectives.Trim()
        
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

# 6. Kopiraj DLL-ove u bin direktorijume
Write-Host "Kopiram DLL-ove..."
$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

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
