# PowerShell skripta za brisanje svih bin i obj direktorijuma

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
Write-Host "Sada možete otvoriti Visual Studio i pokušati Build -> Rebuild Solution"
