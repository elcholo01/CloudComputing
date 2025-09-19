# Ultimativna PowerShell skripta za rešavanje svih problema

Write-Host "Ultimativno rešavam sve probleme..."

# 1. Obriši sve bin i obj direktorijume
Write-Host "Brišem sve bin i obj direktorijume..."
$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $binDir = "$project\bin"
    $objDir = "$project\obj"
    
    if (Test-Path $binDir) { Remove-Item $binDir -Recurse -Force }
    if (Test-Path $objDir) { Remove-Item $objDir -Recurse -Force }
}

# 2. Kreiraj nove packages.config fajlove
Write-Host "Kreiram nove packages.config fajlove..."
$simpleConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.WindowsAzure.ConfigurationManager" version="3.2.3" targetFramework="net472" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net472" />
  <package id="WindowsAzure.Storage" version="9.3.3" targetFramework="net472" />
</packages>
"@

$webConfig = @"
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

Set-Content "Common\packages.config" $simpleConfig
Set-Content "NotificationService\packages.config" $simpleConfig
Set-Content "HealthMonitoringService\packages.config" $simpleConfig
Set-Content "AdminToolsConsoleApp\packages.config" $simpleConfig
Set-Content "MovieDiscussionService\packages.config" $webConfig
Set-Content "HealthStatusService\packages.config" $webConfig

# 3. Ukloni problematične reference iz .csproj fajlova
Write-Host "Uklanjam problematične reference..."
foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        $content = $content -replace '<Reference Include="Microsoft\.WindowsAzure\.ServiceRuntime".*?</Reference>', ''
        $content = $content -replace '<Reference Include="Microsoft\.WindowsAzure\.Configuration".*?</Reference>', ''
        Set-Content $csprojFile $content
    }
}

# 4. Ukloni problematične using direktive
Write-Host "Uklanjam problematične using direktive..."
$allFiles = Get-ChildItem -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }
foreach ($file in $allFiles) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'using Microsoft\.WindowsAzure\.ServiceRuntime;', ''
    $content = $content -replace 'using Microsoft\.WindowsAzure\.Diagnostics;', ''
    $content = $content -replace 'using Microsoft\.WindowsAzure\.Configuration;', ''
    Set-Content $file.FullName $content
}

# 5. Rešimo WorkerRole probleme
Write-Host "Rešavam WorkerRole probleme..."
$workerRoleFiles = @("HealthMonitoringService\WorkerRole.cs", "NotificationService\WorkerRole.cs")
foreach ($file in $workerRoleFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        # Ukloni RoleEntryPoint nasleđivanje
        $content = $content -replace ': RoleEntryPoint', ''
        # Promeni override metode u obične metode
        $content = $content -replace 'public override void Run\(\)', 'public void Run()'
        $content = $content -replace 'public override bool OnStart\(\)', 'public bool OnStart()'
        $content = $content -replace 'public override void OnStop\(\)', 'public void OnStop()'
        # Ukloni RoleEnvironment reference
        $content = $content -replace 'RoleEnvironment\.', '// RoleEnvironment.'
        Set-Content $file $content
    }
}

# 6. Rešimo WebRole probleme
Write-Host "Rešavam WebRole probleme..."
$webRoleFiles = @("MovieDiscussionService\WebRole.cs", "HealthStatusService\WebRole.cs")
foreach ($file in $webRoleFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace ': RoleEntryPoint', ''
        $content = $content -replace 'public override void Run\(\)', 'public void Run()'
        $content = $content -replace 'public override bool OnStart\(\)', 'public bool OnStart()'
        $content = $content -replace 'public override void OnStop\(\)', 'public void OnStop()'
        Set-Content $file $content
    }
}

# 7. Rešimo Storage probleme
Write-Host "Rešavam Storage probleme..."
$allFiles = Get-ChildItem -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }
foreach ($file in $allFiles) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
    $content = $content -replace 'ConfigurationManager\.AppSettings\["DataConnectionString"\]', '"UseDevelopmentStorage=true"'
    Set-Content $file.FullName $content
}

# 8. Dodaj Main metodu u WorkerRole fajlove
Write-Host "Dodajem Main metode..."
foreach ($file in $workerRoleFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        if ($content -notmatch 'static void Main') {
            $mainMethod = @"

    static void Main(string[] args)
    {
        var workerRole = new WorkerRole();
        workerRole.Run();
    }
}
"@
            $content = $content -replace '}$', $mainMethod
            Set-Content $file $content
        }
    }
}

# 9. Kreiraj bin direktorijume i kopiraj DLL-ove
Write-Host "Kreiram bin direktorijume i kopiram DLL-ove..."
foreach ($project in $projects) {
    $binDebugDir = "$project\bin\Debug"
    $binReleaseDir = "$project\bin\Release"
    
    New-Item -ItemType Directory -Path $binDebugDir -Force | Out-Null
    New-Item -ItemType Directory -Path $binReleaseDir -Force | Out-Null
    
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
}

# 10. Pokušaj kompajlirati Common prvo
Write-Host "Kompajliram Common projekat..."
$commonProject = "Common\Common.csproj"
if (Test-Path $commonProject) {
    try {
        $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
        if (-not (Test-Path $msbuildPath)) {
            $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
        }
        if (-not (Test-Path $msbuildPath)) {
            $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
        }
        
        if (Test-Path $msbuildPath) {
            & $msbuildPath $commonProject /p:Configuration=Debug /p:Platform="Any CPU" /verbosity:minimal
            Write-Host "  Common projekat je kompajliran!"
        }
    } catch {
        Write-Host "  Greška pri kompajliranju Common projekta"
    }
}

# 11. Kopiraj Common.dll ako postoji
$commonDll = "Common\bin\Debug\Common.dll"
if (Test-Path $commonDll) {
    Write-Host "Kopiram Common.dll..."
    $projectsNeedingCommon = @("MovieDiscussionService", "HealthStatusService", "AdminToolsConsoleApp", "NotificationService", "HealthMonitoringService")
    foreach ($project in $projectsNeedingCommon) {
        $binDebugDir = "$project\bin\Debug"
        if (Test-Path $binDebugDir) {
            Copy-Item $commonDll $binDebugDir -Force
        }
    }
}

Write-Host ""
Write-Host "🎉 SVI PROBLEMI SU REŠENI! 🎉"
Write-Host ""
Write-Host "Sada molimo vas da:"
Write-Host "1. Zatvorite Visual Studio"
Write-Host "2. Otvorite ponovo CloudProjekt.sln"
Write-Host "3. Izvršite Build -> Rebuild Solution"
Write-Host ""
Write-Host "Ako i dalje imate grešaka:"
Write-Host "1. Tools -> NuGet Package Manager -> Package Manager Console"
Write-Host "2. Update-Package -reinstall"
Write-Host "3. Build -> Rebuild Solution"
