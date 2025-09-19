# PowerShell skripta za popravku Azure Storage referenci i using direktiva

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

# Prvo dodajemo reference u .csproj fajlove
foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    
    if (Test-Path $csprojFile) {
        Write-Host "Ažuriram reference u $project..."
        
        # Učitaj XML fajl
        [xml]$xml = Get-Content $csprojFile
        
        # Pronađi ili kreiraj ItemGroup za Reference-e
        $referenceItemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
        if (-not $referenceItemGroup) {
            $referenceItemGroup = $xml.CreateElement("ItemGroup")
            $xml.Project.AppendChild($referenceItemGroup)
        }
        
        # Dodaj WindowsAzure.Storage referencu
        $storageRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*WindowsAzure.Storage*" }
        if (-not $storageRef) {
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Microsoft.WindowsAzure.Storage")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll"
            $newRef.AppendChild($hintPath)
            $referenceItemGroup.AppendChild($newRef)
            Write-Host "  Dodao WindowsAzure.Storage"
        }
        
        # Dodaj Newtonsoft.Json referencu
        $jsonRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Newtonsoft.Json*" }
        if (-not $jsonRef) {
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Newtonsoft.Json")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll"
            $newRef.AppendChild($hintPath)
            $referenceItemGroup.AppendChild($newRef)
            Write-Host "  Dodao Newtonsoft.Json"
        }
        
        # Dodaj Microsoft.WindowsAzure.Configuration referencu
        $configRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Microsoft.WindowsAzure.Configuration*" }
        if (-not $configRef) {
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Microsoft.WindowsAzure.Configuration")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "packages\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll"
            $newRef.AppendChild($hintPath)
            $referenceItemGroup.AppendChild($newRef)
            Write-Host "  Dodao Microsoft.WindowsAzure.Configuration"
        }
        
        # Dodaj System.ComponentModel.DataAnnotations referencu
        $dataAnnotationsRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*System.ComponentModel.DataAnnotations*" }
        if (-not $dataAnnotationsRef) {
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "System.ComponentModel.DataAnnotations")
            $referenceItemGroup.AppendChild($newRef)
            Write-Host "  Dodao System.ComponentModel.DataAnnotations"
        }
        
        # Sačuvaj promene
        $xml.Save($csprojFile)
        Write-Host "$project reference ažurirane"
    }
}

# Sada dodajemo using direktive u .cs fajlove
$csFiles = Get-ChildItem -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

foreach ($csFile in $csFiles) {
    $content = Get-Content $csFile.FullName -Raw
    $needsUpdate = $false
    $newContent = $content
    
    # Proveri da li fajl koristi Azure Storage klase
    $usesAzureStorage = $content -match "CloudStorageAccount|CloudTable|TableEntity|TableOperation|TableQuery|PartitionKey|RowKey|Timestamp"
    
    if ($usesAzureStorage) {
        Write-Host "Proveravam using direktive u $($csFile.Name)..."
        
        # Dodaj Microsoft.WindowsAzure.Storage using ako nedostaje
        if ($content -notmatch "using Microsoft\.WindowsAzure\.Storage;") {
            $newContent = $newContent -replace "using System;", "using System;`nusing Microsoft.WindowsAzure.Storage;"
            $needsUpdate = $true
            Write-Host "  Dodao Microsoft.WindowsAzure.Storage using"
        }
        
        # Dodaj Microsoft.WindowsAzure.Storage.Table using ako nedostaje
        if ($content -notmatch "using Microsoft\.WindowsAzure\.Storage\.Table;") {
            $newContent = $newContent -replace "using Microsoft\.WindowsAzure\.Storage;", "using Microsoft.WindowsAzure.Storage;`nusing Microsoft.WindowsAzure.Storage.Table;"
            $needsUpdate = $true
            Write-Host "  Dodao Microsoft.WindowsAzure.Storage.Table using"
        }
        
        # Dodaj Microsoft.WindowsAzure.Storage.Queue using ako koristi Queue
        if ($content -match "CloudQueue" -and $content -notmatch "using Microsoft\.WindowsAzure\.Storage\.Queue;") {
            $newContent = $newContent -replace "using Microsoft\.WindowsAzure\.Storage\.Table;", "using Microsoft.WindowsAzure.Storage.Table;`nusing Microsoft.WindowsAzure.Storage.Queue;"
            $needsUpdate = $true
            Write-Host "  Dodao Microsoft.WindowsAzure.Storage.Queue using"
        }
        
        if ($needsUpdate) {
            Set-Content -Path $csFile.FullName -Value $newContent -Encoding UTF8
            Write-Host "  $($csFile.Name) ažuriran"
        }
    }
}

Write-Host "`nSvi projekti su ažurirani!"
Write-Host "Sada zatvorite Visual Studio i otvorite ponovo CloudProjekt.sln"
Write-Host "Zatim uradite Clean Solution i Rebuild Solution"
