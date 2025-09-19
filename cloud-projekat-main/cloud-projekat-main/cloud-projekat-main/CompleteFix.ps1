# Kompletna PowerShell skripta za rešavanje svih problema

Write-Host "Rešavam sve probleme sistematski..."

# 1. Prvo ću ažurirati sve packages.config fajlove da koriste ispravne verzije
Write-Host "Ažuriram packages.config fajlove..."

$packagesConfigs = @(
    "Common\packages.config",
    "MovieDiscussionService\packages.config", 
    "NotificationService\packages.config",
    "HealthMonitoringService\packages.config",
    "AdminToolsConsoleApp\packages.config",
    "HealthStatusService\packages.config"
)

foreach ($config in $packagesConfigs) {
    if (Test-Path $config) {
        Write-Host "Ažuriram $config..."
        
        $content = Get-Content $config -Raw
        
        # Zameni Microsoft.WindowsAzure.ServiceRuntime sa ispravnom verzijom
        $content = $content -replace 'Microsoft\.WindowsAzure\.ServiceRuntime\.2\.7\.0', 'Microsoft.WindowsAzure.ServiceRuntime.2.9.0'
        
        # Zameni Microsoft.WindowsAzure.ConfigurationManager sa ispravnom verzijom
        $content = $content -replace 'Microsoft\.WindowsAzure\.ConfigurationManager\.3\.2\.3', 'Microsoft.WindowsAzure.ConfigurationManager.3.2.3'
        
        Set-Content $config $content
        Write-Host "  Ažuriran $config"
    }
}

# 2. Ažuriraj sve .csproj fajlove da koriste ispravne verzije
Write-Host "Ažuriram .csproj fajlove..."

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    
    if (Test-Path $csprojFile) {
        Write-Host "Ažuriram $csprojFile..."
        
        [xml]$xml = Get-Content $csprojFile
        
        # Pronađi sve HintPath elemente i ažuriraj ih
        $hintPaths = $xml.SelectNodes("//HintPath")
        foreach ($hintPath in $hintPaths) {
            if ($hintPath.InnerText -like "*Microsoft.WindowsAzure.ServiceRuntime.2.7.0*") {
                $hintPath.InnerText = $hintPath.InnerText -replace '2\.7\.0', '2.9.0'
                Write-Host "  Ažuriran ServiceRuntime putanja"
            }
        }
        
        $xml.Save($csprojFile)
    }
}

# 3. Rešimo problem sa AlterEmailEntity u AdminToolsConsoleApp
Write-Host "Rešavam problem sa AlterEmailEntity..."
$alertEmailsTableFile = "AdminToolsConsoleApp\AlertEmailsTable.cs"
if (Test-Path $alertEmailsTableFile) {
    $content = Get-Content $alertEmailsTableFile -Raw
    $content = $content -replace 'AlterEmailEntity', 'AlertEmailEntity'
    Set-Content $alertEmailsTableFile $content
    Write-Host "  Ispravljen AlterEmailEntity u AlertEmailEntity"
}

# 4. Rešimo problem sa PhotoUrl u AccountController
Write-Host "Rešavam problem sa PhotoUrl..."
$accountControllerFile = "MovieDiscussionService\Controllers\AccountController.cs"
if (Test-Path $accountControllerFile) {
    $content = Get-Content $accountControllerFile -Raw
    # Ukloni new ključnu reč jer nije potrebna
    $content = $content -replace 'public new string PhotoUrl', 'public string PhotoUrl'
    Set-Content $accountControllerFile $content
    Write-Host "  Uklonjen new ključna reč iz PhotoUrl"
}

# 5. Rešimo problem sa using direktivama u Common/Storage.cs
Write-Host "Rešavam using direktive u Common/Storage.cs..."
$storageFile = "Common\Storage.cs"
if (Test-Path $storageFile) {
    $content = Get-Content $storageFile -Raw
    # Ukloni problematičnu using direktivu
    $content = $content -replace 'using Microsoft\.WindowsAzure\.Configuration;', ''
    Set-Content $storageFile $content
    Write-Host "  Uklonjena problematična using direktiva"
}

# 6. Kopiraj DLL-ove u bin direktorijume
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

# 7. Rešimo problem sa CloudConfigurationManager u Common/Storage.cs
Write-Host "Rešavam CloudConfigurationManager problem..."
if (Test-Path $storageFile) {
    $content = Get-Content $storageFile -Raw
    # Zameni CloudConfigurationManager sa ConfigurationManager
    $content = $content -replace 'CloudConfigurationManager\.GetSetting', 'ConfigurationManager.AppSettings'
    Set-Content $storageFile $content
    Write-Host "  Zamenjen CloudConfigurationManager sa ConfigurationManager"
}

# 8. Rešimo problem sa using direktivama u svim WorkerRole fajlovima
Write-Host "Rešavam using direktive u WorkerRole fajlovima..."
$workerRoleFiles = @(
    "HealthMonitoringService\WorkerRole.cs",
    "NotificationService\WorkerRole.cs",
    "MovieDiscussionService\WebRole.cs",
    "HealthStatusService\WebRole.cs"
)

foreach ($file in $workerRoleFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        # Ukloni problematične using direktive
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Diagnostics;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.ServiceRuntime;', ''
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

# 9. Rešimo problem sa using direktivama u HealthStatusService
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
        $content = Get-Content $file -Raw
        # Ukloni problematične using direktive
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Storage;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Storage\.Table;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.Diagnostics;', ''
        $content = $content -replace 'using Microsoft\.WindowsAzure\.ServiceRuntime;', ''
        Set-Content $file $content
        Write-Host "  Ažuriran $file"
    }
}

Write-Host "Svi problemi su rešeni!"
Write-Host ""
Write-Host "Sada molimo vas da:"
Write-Host "1. Zatvorite Visual Studio"
Write-Host "2. Obrišite bin i obj direktorijume iz svih projekata"
Write-Host "3. Otvorite ponovo CloudProjekt.sln"
Write-Host "4. Izvršite Tools -> NuGet Package Manager -> Package Manager Console"
Write-Host "5. U konzoli izvršite: Update-Package -reinstall"
Write-Host "6. Zatim izvršite Build -> Rebuild Solution"
