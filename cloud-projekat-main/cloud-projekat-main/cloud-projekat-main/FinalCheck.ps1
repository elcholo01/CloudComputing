# PowerShell skripta za finalnu proveru Azure Storage grešaka

Write-Host "Proveravam da li su sve Azure Storage greške rešene..."
Write-Host ""

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

$totalErrors = 0

foreach ($project in $projects) {
    Write-Host "=== $project ==="
    
    # Proveri .csproj reference
    $csprojFile = "$project\$project.csproj"
    if (Test-Path $csprojFile) {
        $content = Get-Content $csprojFile -Raw
        
        $hasStorageRef = $content -match "Microsoft\.WindowsAzure\.Storage"
        $hasConfigRef = $content -match "Microsoft\.WindowsAzure\.Configuration"
        $hasJsonRef = $content -match "Newtonsoft\.Json"
        
        if ($hasStorageRef) {
            Write-Host "  OK WindowsAzure.Storage referenca"
        } else {
            Write-Host "  MISSING WindowsAzure.Storage referenca"
            $totalErrors++
        }
        
        if ($hasConfigRef) {
            Write-Host "  OK WindowsAzure.Configuration referenca"
        } else {
            Write-Host "  MISSING WindowsAzure.Configuration referenca"
            $totalErrors++
        }
        
        if ($hasJsonRef) {
            Write-Host "  OK Newtonsoft.Json referenca"
        } else {
            Write-Host "  MISSING Newtonsoft.Json referenca"
            $totalErrors++
        }
    }
    
    # Proveri .cs fajlove za using direktive
    $csFiles = Get-ChildItem -Path $project -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }
    
    foreach ($csFile in $csFiles) {
        $content = Get-Content $csFile.FullName -Raw
        
        # Proveri da li fajl koristi Azure Storage klase
        $usesAzureStorage = $content -match "CloudStorageAccount|CloudTable|TableEntity|TableOperation|TableQuery|PartitionKey|RowKey|Timestamp"
        
        if ($usesAzureStorage) {
            $hasStorageUsing = $content -match "Microsoft\.WindowsAzure\.Storage;"
            $hasTableUsing = $content -match "Microsoft\.WindowsAzure\.Storage\.Table;"
            $hasQueueUsing = $content -match "Microsoft\.WindowsAzure\.Storage\.Queue;"
            
            if (-not $hasStorageUsing) {
                Write-Host "  MISSING $($csFile.Name) - nedostaje Microsoft.WindowsAzure.Storage using"
                $totalErrors++
            }
            
            if (-not $hasTableUsing) {
                Write-Host "  MISSING $($csFile.Name) - nedostaje Microsoft.WindowsAzure.Storage.Table using"
                $totalErrors++
            }
            
            if ($content -match "CloudQueue" -and -not $hasQueueUsing) {
                Write-Host "  MISSING $($csFile.Name) - nedostaje Microsoft.WindowsAzure.Storage.Queue using"
                $totalErrors++
            }
        }
    }
    
    Write-Host ""
}

Write-Host "=== REZIME ==="
if ($totalErrors -eq 0) {
    Write-Host "OK Sve greške su rešene! Možete pokušati da build-ujete solution."
} else {
    Write-Host "ERROR Pronađeno je $totalErrors grešaka koje treba da se reše."
}

Write-Host ""
Write-Host "NEXT STEPS:"
Write-Host "1. Zatvorite Visual Studio"
Write-Host "2. Otvorite ponovo CloudProjekt.sln"
Write-Host "3. Uradite Clean Solution (Build -> Clean Solution)"
Write-Host "4. Uradite Rebuild Solution (Build -> Rebuild Solution)"
Write-Host "5. Ako i dalje ima grešaka, proverite NuGet pakete"
