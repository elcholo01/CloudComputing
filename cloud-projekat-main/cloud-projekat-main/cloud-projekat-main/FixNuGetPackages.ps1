# PowerShell skripta za rešavanje problema sa NuGet paketima

Write-Host "Rešavam probleme sa NuGet paketima..."

# 1. Ažuriraj sve packages.config fajlove da koriste ispravne verzije
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
        $content = $content -replace 'Microsoft\.WindowsAzure\.ServiceRuntime\.2\.9\.0', 'Microsoft.WindowsAzure.ServiceRuntime.2.9.0'
        
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
            if ($hintPath.InnerText -like "*Microsoft.WindowsAzure.ServiceRuntime*") {
                $hintPath.InnerText = $hintPath.InnerText -replace '2\.7\.0', '2.9.0'
                Write-Host "  Ažuriran ServiceRuntime putanja"
            }
        }
        
        $xml.Save($csprojFile)
    }
}

# 3. Kreiraj novi packages.config za HealthStatusService koji koristi ispravne verzije
Write-Host "Kreiram novi packages.config za HealthStatusService..."
$healthStatusPackagesConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.AspNet.Mvc" version="5.2.9" targetFramework="net472" />
  <package id="Microsoft.AspNet.Razor" version="3.2.9" targetFramework="net472" />
  <package id="Microsoft.AspNet.Web.Optimization" version="1.1.3" targetFramework="net472" />
  <package id="Microsoft.AspNet.WebPages" version="3.2.9" targetFramework="net472" />
  <package id="Microsoft.CodeDom.Providers.DotNetCompilerPlatform" version="2.0.1" targetFramework="net472" />
  <package id="Microsoft.Web.Infrastructure" version="2.0.0" targetFramework="net472" />
  <package id="Microsoft.WindowsAzure.ConfigurationManager" version="3.2.3" targetFramework="net472" />
  <package id="Microsoft.WindowsAzure.ServiceRuntime" version="2.9.0" targetFramework="net472" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
  <package id="WebGrease" version="1.6.0" targetFramework="net472" />
  <package id="WindowsAzure.Storage" version="9.3.3" targetFramework="net472" />
</packages>
"@

Set-Content "HealthStatusService\packages.config" $healthStatusPackagesConfig
Write-Host "  Kreiran novi packages.config za HealthStatusService"

# 4. Kreiraj novi packages.config za MovieDiscussionService
Write-Host "Kreiram novi packages.config za MovieDiscussionService..."
$movieDiscussionPackagesConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.AspNet.Mvc" version="5.2.9" targetFramework="net472" />
  <package id="Microsoft.AspNet.Razor" version="3.2.9" targetFramework="net472" />
  <package id="Microsoft.AspNet.Web.Optimization" version="1.1.3" targetFramework="net472" />
  <package id="Microsoft.AspNet.WebPages" version="3.2.9" targetFramework="net472" />
  <package id="Microsoft.CodeDom.Providers.DotNetCompilerPlatform" version="2.0.1" targetFramework="net472" />
  <package id="Microsoft.Web.Infrastructure" version="2.0.0" targetFramework="net472" />
  <package id="Microsoft.WindowsAzure.ConfigurationManager" version="3.2.3" targetFramework="net472" />
  <package id="Microsoft.WindowsAzure.ServiceRuntime" version="2.9.0" targetFramework="net472" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
  <package id="WebGrease" version="1.6.0" targetFramework="net472" />
  <package id="WindowsAzure.Storage" version="9.3.3" targetFramework="net472" />
</packages>
"@

Set-Content "MovieDiscussionService\packages.config" $movieDiscussionPackagesConfig
Write-Host "  Kreiran novi packages.config za MovieDiscussionService"

# 5. Kreiraj novi packages.config za ostale projekte
Write-Host "Kreiram nove packages.config fajlove za ostale projekte..."

$simplePackagesConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.WindowsAzure.ConfigurationManager" version="3.2.3" targetFramework="net472" />
  <package id="Microsoft.WindowsAzure.ServiceRuntime" version="2.9.0" targetFramework="net472" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
  <package id="WindowsAzure.Storage" version="9.3.3" targetFramework="net472" />
</packages>
"@

Set-Content "Common\packages.config" $simplePackagesConfig
Set-Content "NotificationService\packages.config" $simplePackagesConfig
Set-Content "HealthMonitoringService\packages.config" $simplePackagesConfig
Set-Content "AdminToolsConsoleApp\packages.config" $simplePackagesConfig

Write-Host "  Kreirani novi packages.config fajlovi za sve projekte"

Write-Host "Svi problemi sa NuGet paketima su rešeni!"
Write-Host ""
Write-Host "Sada molimo vas da:"
Write-Host "1. Zatvorite Visual Studio"
Write-Host "2. Otvorite ponovo CloudProjekt.sln"
Write-Host "3. Izvršite Tools -> NuGet Package Manager -> Package Manager Console"
Write-Host "4. U konzoli izvršite: Update-Package -reinstall"
Write-Host "5. Zatim izvršite Build -> Rebuild Solution"
