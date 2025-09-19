# PowerShell skripta za rešavanje problema sa using direktivama

Write-Host "Rešavam probleme sa using direktivama..."

# 1. Rešimo problem sa using direktivama u HealthStatusService
Write-Host "Rešavam using direktive u HealthStatusService..."

$healthStatusFiles = @(
    "HealthStatusService\Controllers\HomeController.cs",
    "HealthStatusService\App_Start\BundleConfig.cs",
    "HealthStatusService\App_Start\FilterConfig.cs",
    "HealthStatusService\App_Start\RouteConfig.cs",
    "HealthStatusService\Global.asax.cs",
    "HealthStatusService\WebRole.cs"
)

foreach ($file in $healthStatusFiles) {
    if (Test-Path $file) {
        Write-Host "Ažuriram $file..."
        
        $content = Get-Content $file -Raw
        
        # Dodaj potrebne using direktive na početak fajla
        $newUsingDirectives = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.ServiceRuntime;

"@
        
        # Zameni postojeće using direktive
        $content = $content -replace 'using System;.*?using System\.Web;', $newUsingDirectives.Trim()
        
        # Ukloni problematične using direktive
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Storage;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Storage\.Table;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Diagnostics;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.ServiceRuntime;', ''
        $content = $content -replace 'using System\.Web\.Mvc;', ''
        $content = $content -replace 'using System\.Web\.Optimization;', ''
        
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

# 2. Rešimo problem sa using direktivama u WorkerRole fajlovima
Write-Host "Rešavam using direktive u WorkerRole fajlovima..."

$workerRoleFiles = @(
    "HealthMonitoringService\WorkerRole.cs",
    "NotificationService\WorkerRole.cs",
    "MovieDiscussionService\WebRole.cs"
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
        
        # Ukloni problematične using direktive
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Diagnostics;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.ServiceRuntime;', ''
        
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

# 3. Rešimo problem sa using direktivama u Common/Storage.cs
Write-Host "Rešavam using direktive u Common/Storage.cs..."
$storageFile = "Common\Storage.cs"
if (Test-Path $storageFile) {
    $content = Get-Content $storageFile -Raw
    
    # Dodaj potrebne using direktive
    $newUsingDirectives = @"
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

"@
    
    # Zameni postojeće using direktive
    $content = $content -replace 'using System;.*?using System\.Threading\.Tasks;', $newUsingDirectives.Trim()
    
    # Ukloni problematične using direktive
    $content = $content -replace 'using Microsoft\.WindowsAzure;', ''
    $content = $content -replace 'using Microsoft\.WindowsAzure\.Configuration;', ''
    
    Set-Content $storageFile $content
    Write-Host "  Ažuriran $storageFile"
}

# 4. Rešimo problem sa using direktivama u AdminToolsConsoleApp
Write-Host "Rešavam using direktive u AdminToolsConsoleApp..."
$adminFiles = @(
    "AdminToolsConsoleApp\Program.cs",
    "AdminToolsConsoleApp\AlertEmailsTable.cs",
    "AdminToolsConsoleApp\UsersTable.cs"
)

foreach ($file in $adminFiles) {
    if (Test-Path $file) {
        Write-Host "Ažuriram $file..."
        
        $content = Get-Content $file -Raw
        
        # Dodaj potrebne using direktive
        $newUsingDirectives = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

"@
        
        # Zameni postojeće using direktive
        $content = $content -replace 'using System;.*?using System\.Threading\.Tasks;', $newUsingDirectives.Trim()
        
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

Write-Host "Svi problemi sa using direktivama su rešeni!"
Write-Host ""
Write-Host "Sada molimo vas da:"
Write-Host "1. Zatvorite Visual Studio"
Write-Host "2. Otvorite ponovo CloudProjekt.sln"
Write-Host "3. Izvršite Build -> Rebuild Solution"
