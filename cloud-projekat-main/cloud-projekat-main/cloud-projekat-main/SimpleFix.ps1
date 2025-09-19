# Jednostavna skripta za popravku

Write-Host "Popravljam projekat..." -ForegroundColor Green

# Popravi HealthCheckEntity.Timestamp
$healthController = "HealthStatusService\Controllers\HomeController.cs"
if (Test-Path $healthController) {
    $content = Get-Content $healthController -Raw
    $content = $content -replace 'public DateTime Timestamp', 'public new DateTime Timestamp'
    Set-Content $healthController $content -Encoding UTF8
    Write-Host "Popravljen HealthCheckEntity.Timestamp" -ForegroundColor Green
}

# Popravi verzije u svim .csproj fajlovima
$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        
        # Popravi Azure Storage verziju
        $content = $content -replace 'Version=9\.3\.2\.0', 'Version=9.3.3.0'
        
        # Popravi Newtonsoft.Json verziju  
        $content = $content -replace 'Version=13\.0\.0\.0', 'Version=13.0.3.0'
        
        Set-Content $csprojFile $content -Encoding UTF8
        Write-Host "Popravljen $project" -ForegroundColor Green
    }
}

Write-Host "Gotovo!" -ForegroundColor Green
