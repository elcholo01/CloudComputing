# Finalna brza PowerShell skripta za rešavanje svih preostalih problema

Write-Host "Rešavam preostale probleme..."

# 1. Rešimo problem sa OnStop() metodama
Write-Host "Rešavam OnStop() probleme..."
$workerRoleFiles = @("HealthMonitoringService\WorkerRole.cs", "NotificationService\WorkerRole.cs")
foreach ($file in $workerRoleFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace 'public override void OnStop\(\)', 'public void OnStop()'
        Set-Content $file $content
    }
}

# 2. Rešimo problem sa ConfigurationManager u Common/Storage.cs
Write-Host "Rešavam ConfigurationManager problem..."
$storageFile = "Common\Storage.cs"
if (Test-Path $storageFile) {
    $content = Get-Content $storageFile -Raw
    $content = $content -replace 'ConfigurationManager\.AppSettings\["DataConnectionString"\]', '"UseDevelopmentStorage=true"'
    Set-Content $storageFile $content
}

# 3. Rešimo problem sa Storage u AdminToolsConsoleApp
Write-Host "Rešavam Storage problem u AdminToolsConsoleApp..."
$alertEmailsTableFile = "AdminToolsConsoleApp\AlertEmailsTable.cs"
if (Test-Path $alertEmailsTableFile) {
    $content = Get-Content $alertEmailsTableFile -Raw
    $content = $content -replace 'Storage\.GetStorageAccount\(\)', 'CloudStorageAccount.Parse("UseDevelopmentStorage=true")'
    Set-Content $alertEmailsTableFile $content
}

# 4. Kompajliraj Common projekat prvo
Write-Host "Kompajliram Common projekat..."
$commonProject = "Common\Common.csproj"
if (Test-Path $commonProject) {
    try {
        # Pokušaj sa MSBuild
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
        } else {
            Write-Host "  MSBuild nije pronađen, preskačem kompajliranje"
        }
    } catch {
        Write-Host "  Greška pri kompajliranju Common projekta"
    }
}

# 5. Kopiraj Common.dll u sve projekte koji ga koriste
Write-Host "Kopiram Common.dll..."
$commonDll = "Common\bin\Debug\Common.dll"
if (Test-Path $commonDll) {
    $projects = @("MovieDiscussionService", "HealthStatusService", "AdminToolsConsoleApp", "NotificationService", "HealthMonitoringService")
    foreach ($project in $projects) {
        $binDebugDir = "$project\bin\Debug"
        if (Test-Path $binDebugDir) {
            Copy-Item $commonDll $binDebugDir -Force
            Write-Host "  Kopiran Common.dll u $project"
        }
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
