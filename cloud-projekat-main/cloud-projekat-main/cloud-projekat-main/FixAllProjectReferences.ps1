# PowerShell skripta za popravku svih .csproj fajlova

Write-Host "Popravljam reference u svim .csproj fajlovima..."

$projects = @("MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    Write-Host "=== Popravljam $project ==="
    
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        
        # Popravi Microsoft.WindowsAzure.Storage referencu
        $content = $content -replace '<Reference Include="Microsoft\.WindowsAzure\.Storage" xmlns="">\s*<HintPath>\.\.\\packages\\WindowsAzure\.Storage\.9\.3\.3\\lib\\net45\\Microsoft\.WindowsAzure\.Storage\.dll</HintPath>\s*<Private>True</Private>\s*<SpecificVersion>True</SpecificVersion>\s*</Reference>', '<Reference Include="Microsoft.WindowsAzure.Storage"><HintPath>..\packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll</HintPath><Private>True</Private></Reference>'
        
        # Popravi Newtonsoft.Json referencu
        $content = $content -replace '<Reference Include="Newtonsoft\.Json" xmlns="">\s*<HintPath>\.\.\\packages\\Newtonsoft\.Json\.13\.0\.3\\lib\\net45\\Newtonsoft\.Json\.dll</HintPath>\s*<Private>True</Private>\s*<SpecificVersion>True</SpecificVersion>\s*</Reference>', '<Reference Include="Newtonsoft.Json"><HintPath>..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll</HintPath><Private>True</Private></Reference>'
        
        # Popravi Microsoft.WindowsAzure.Configuration referencu
        $content = $content -replace '<Reference Include="Microsoft\.WindowsAzure\.Configuration" xmlns="">\s*<HintPath>\.\.\\packages\\Microsoft\.WindowsAzure\.ConfigurationManager\.3\.2\.3\\lib\\net40\\Microsoft\.WindowsAzure\.Configuration\.dll</HintPath>\s*</Reference>', '<Reference Include="Microsoft.WindowsAzure.Configuration"><HintPath>..\packages\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll</HintPath><Private>True</Private></Reference>'
        
        # Dodaj System.Net.Mail referencu ako ne postoji
        if ($content -notmatch 'System\.Net\.Mail') {
            $content = $content -replace '(<Reference Include="System\.Net\.Http">\s*</Reference>)', '$1<Reference Include="System.Net.Mail" />'
        }
        
        Set-Content $csprojFile -Value $content -Encoding UTF8
        Write-Host "  OK $project.csproj popravljen"
    } else {
        Write-Host "  ERROR $csprojFile nije pronađen"
    }
}

Write-Host ""
Write-Host "Sve reference su popravljene!"
Write-Host "Sada možete pokušati build ponovo."
