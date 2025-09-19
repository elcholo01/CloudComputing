# PowerShell skripta za rešavanje svih problema

Write-Host "=== Rešavam sve probleme u projektu ===" -ForegroundColor Green

# 1. Dodaj System.Net.Mail reference u sve projekte
Write-Host "1. Dodajem System.Net.Mail reference..." -ForegroundColor Yellow

$projects = @("NotificationService", "AdminToolsConsoleApp")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        
        # Dodaj System.Net.Mail referencu ako ne postoji
        if ($content -notmatch "System\.Net\.Mail") {
            $referenceToAdd = @"
    <Reference Include="System.Net.Mail">
      <HintPath>..\packages\System.Net.Mail.1.0.0\lib\net45\System.Net.Mail.dll</HintPath>
    </Reference>
"@
            
            # Pronađi poslednju referencu i dodaj novu
            $content = $content -replace '(\s*</ItemGroup>)', "$referenceToAdd`n`$1"
            Set-Content $csprojFile $content -Encoding UTF8
            Write-Host "  ✓ Dodao System.Net.Mail u $project" -ForegroundColor Green
        }
    }
}

# 2. Popravi WebGrease reference
Write-Host "2. Popravljam WebGrease reference..." -ForegroundColor Yellow

$webProjects = @("MovieDiscussionService", "HealthStatusService")

foreach ($project in $webProjects) {
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        
        # Ukloni stari WebGrease i dodaj novi
        $content = $content -replace '<Reference Include="WebGrease[^"]*"[^>]*>.*?</Reference>', ''
        
        # Dodaj novi WebGrease
        $webGreaseRef = @"
    <Reference Include="WebGrease">
      <HintPath>..\packages\WebGrease.1.6.0\lib\WebGrease.dll</HintPath>
    </Reference>
"@
        
        $content = $content -replace '(\s*</ItemGroup>)', "$webGreaseRef`n`$1"
        Set-Content $csprojFile $content -Encoding UTF8
        Write-Host "  ✓ Popravljen WebGrease u $project" -ForegroundColor Green
    }
}

# 3. Popravi MVC reference u HealthStatusService
Write-Host "3. Popravljam MVC reference..." -ForegroundColor Yellow

$mvcProject = "HealthStatusService"
$csprojFile = "$mvcProject\$mvcProject.csproj"

if (Test-Path $csprojFile) {
    $content = Get-Content $csprojFile -Raw
    
    # Proveri da li MVC reference postoje
    if ($content -match "System\.Web\.Mvc") {
        Write-Host "  ✓ MVC reference već postoje u $mvcProject" -ForegroundColor Green
    } else {
        # Dodaj MVC reference
        $mvcRefs = @"
    <Reference Include="System.Web.Mvc, Version=5.2.9.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL">
      <HintPath>..\packages\Microsoft.AspNet.Mvc.5.2.9\lib\net45\System.Web.Mvc.dll</HintPath>
    </Reference>
    <Reference Include="System.Web.WebPages, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL">
      <HintPath>..\packages\Microsoft.AspNet.WebPages.3.2.9\lib\net45\System.Web.WebPages.dll</HintPath>
    </Reference>
    <Reference Include="System.Web.WebPages.Deployment, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL">
      <HintPath>..\packages\Microsoft.AspNet.WebPages.3.2.9\lib\net45\System.Web.WebPages.Deployment.dll</HintPath>
    </Reference>
"@
        
        $content = $content -replace '(\s*</ItemGroup>)', "$mvcRefs`n`$1"
        Set-Content $csprojFile $content -Encoding UTF8
        Write-Host "  ✓ Dodao MVC reference u $mvcProject" -ForegroundColor Green
    }
}

# 4. Popravi verzije paketa - koristi najnovije verzije
Write-Host "4. Popravljam verzije paketa..." -ForegroundColor Yellow

$allProjects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $allProjects) {
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        
        # Popravi Azure Storage verziju na 9.3.3
        $content = $content -replace 'WindowsAzure\.Storage\.9\.3\.2', 'WindowsAzure.Storage.9.3.3'
        $content = $content -replace 'Version=9\.3\.2\.0', 'Version=9.3.3.0'
        
        # Popravi Newtonsoft.Json verziju na 13.0.3
        $content = $content -replace 'Newtonsoft\.Json\.13\.0\.0', 'Newtonsoft.Json.13.0.3'
        $content = $content -replace 'Version=13\.0\.0\.0', 'Version=13.0.3.0'
        
        Set-Content $csprojFile $content -Encoding UTF8
        Write-Host "  ✓ Popravljene verzije u $project" -ForegroundColor Green
    }
}

# 5. Popravi HealthCheckEntity.Timestamp warning
Write-Host "5. Popravljam HealthCheckEntity warning..." -ForegroundColor Yellow

$healthController = "HealthStatusService\Controllers\HomeController.cs"
if (Test-Path $healthController) {
    $content = Get-Content $healthController -Raw
    
    # Dodaj 'new' keyword za Timestamp property
    $content = $content -replace 'public DateTime Timestamp', 'public new DateTime Timestamp'
    
    Set-Content $healthController $content -Encoding UTF8
    Write-Host "  ✓ Popravljen HealthCheckEntity.Timestamp" -ForegroundColor Green
}

Write-Host "`n=== Svi problemi su rešeni! ===" -ForegroundColor Green
Write-Host "Sada možete pokušati build ponovo." -ForegroundColor Cyan
