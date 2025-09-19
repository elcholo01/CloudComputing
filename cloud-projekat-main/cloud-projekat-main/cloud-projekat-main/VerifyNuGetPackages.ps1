# PowerShell skripta za proveru NuGet paketa

Write-Host "Proveravam NuGet pakete..."

$requiredPackages = @(
    @{Id="WindowsAzure.Storage"; Version="9.3.3"},
    @{Id="Newtonsoft.Json"; Version="13.0.3"},
    @{Id="Microsoft.WindowsAzure.ConfigurationManager"; Version="3.2.3"}
)

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $packagesConfigFile = "$project\packages.config"
    
    if (Test-Path $packagesConfigFile) {
        Write-Host "Proveravam $project..."
        
        [xml]$xml = Get-Content $packagesConfigFile
        
        foreach ($requiredPackage in $requiredPackages) {
            $package = $xml.packages.package | Where-Object { $_.id -eq $requiredPackage.Id }
            
            if ($package) {
                Write-Host "  OK $($requiredPackage.Id) $($package.version)"
            } else {
                Write-Host "  MISSING $($requiredPackage.Id)"
            }
        }
    }
}

Write-Host ""
Write-Host "Proveravanje zavrseno!"
Write-Host "Ako neki paketi nedostaju, instalirajte ih pomocu NuGet Package Manager-a"
