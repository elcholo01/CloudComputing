# Finalna brza skripta - sve u jednoj komandi

Write-Host "Rešavam sve probleme brzo..."

# 1. Obriši sve bin i obj
Get-ChildItem -Directory | Where-Object { $_.Name -match "^(Common|MovieDiscussionService|NotificationService|HealthMonitoringService|AdminToolsConsoleApp|HealthStatusService)$" } | ForEach-Object { 
    Remove-Item "$($_.Name)\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$($_.Name)\obj" -Recurse -Force -ErrorAction SilentlyContinue
}

# 2. Kreiraj packages.config fajlove
$simple = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.WindowsAzure.ConfigurationManager" version="3.2.3" targetFramework="net472" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
  <package id="WindowsAzure.Storage" version="9.3.3" targetFramework="net472" />
</packages>
"@

$web = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.AspNet.Mvc" version="5.2.9" targetFramework="net472" />
  <package id="Microsoft.AspNet.Razor" version="3.2.9" targetFramework="net472" />
  <package id="Microsoft.AspNet.Web.Optimization" version="1.1.3" targetFramework="net472" />
  <package id="Microsoft.AspNet.WebPages" version="3.2.9" targetFramework="net472" />
  <package id="Microsoft.CodeDom.Providers.DotNetCompilerPlatform" version="2.0.1" targetFramework="net472" />
  <package id="Microsoft.Web.Infrastructure" version="2.0.0" targetFramework="net472" />
  <package id="Microsoft.WindowsAzure.ConfigurationManager" version="3.2.3" targetFramework="net472" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
  <package id="WebGrease" version="1.6.0" targetFramework="net472" />
  <package id="WindowsAzure.Storage" version="9.3.3" targetFramework="net472" />
</packages>
"@

Set-Content "Common\packages.config" $simple
Set-Content "NotificationService\packages.config" $simple
Set-Content "HealthMonitoringService\packages.config" $simple
Set-Content "AdminToolsConsoleApp\packages.config" $simple
Set-Content "MovieDiscussionService\packages.config" $web
Set-Content "HealthStatusService\packages.config" $web

# 3. Kopiraj DLL-ove
$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")
foreach ($project in $projects) {
    New-Item -ItemType Directory -Path "$project\bin\Debug" -Force | Out-Null
    New-Item -ItemType Directory -Path "$project\bin\Release" -Force | Out-Null
    
    if (Test-Path "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll") {
        Copy-Item "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll" "$project\bin\Debug\" -Force
        Copy-Item "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll" "$project\bin\Release\" -Force
    }
    
    if (Test-Path "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll") {
        Copy-Item "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" "$project\bin\Debug\" -Force
        Copy-Item "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" "$project\bin\Release\" -Force
    }
}

Write-Host "GOTOVO! Sada otvorite Visual Studio i Build -> Rebuild Solution"
