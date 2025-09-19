# PowerShell skripta za brisanje svih bin i obj direktorijuma i ponovno kopiranje DLL-ova

Write-Host "Brišem sve bin i obj direktorijume..."

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $binDir = "$project\bin"
    $objDir = "$project\obj"
    
    if (Test-Path $binDir) {
        Remove-Item $binDir -Recurse -Force
        Write-Host "Obrisan $binDir"
    }
    
    if (Test-Path $objDir) {
        Remove-Item $objDir -Recurse -Force
        Write-Host "Obrisan $objDir"
    }
}

Write-Host "Svi bin i obj direktorijumi su obrisani!"

# Kopiraj DLL-ove u bin direktorijume
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
    
    Write-Host "Kopirani DLL-ovi u $project"
}

Write-Host "Svi DLL-ovi su kopirani!"
Write-Host "Sada možete otvoriti Visual Studio i pokušati Build -> Rebuild Solution"
