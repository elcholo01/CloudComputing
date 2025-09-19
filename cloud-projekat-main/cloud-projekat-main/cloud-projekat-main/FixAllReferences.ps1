# PowerShell skripta za potpuno rešavanje problema sa reference-ima

Write-Host "Započinjem potpuno rešavanje problema sa reference-ima..."

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    
    if (Test-Path $csprojFile) {
        Write-Host "Ažuriram $project..."
        
        # Učitaj XML fajl
        [xml]$xml = Get-Content $csprojFile
        
        # Pronađi ili kreiraj ItemGroup za Reference-e
        $referenceItemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
        if (-not $referenceItemGroup) {
            $referenceItemGroup = $xml.CreateElement("ItemGroup")
            $xml.Project.AppendChild($referenceItemGroup)
        }
        
        # Ukloni postojeće reference ako postoje
        $existingStorageRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*WindowsAzure.Storage*" }
        if ($existingStorageRef) {
            $existingStorageRef.ParentNode.RemoveChild($existingStorageRef)
        }
        
        $existingJsonRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Newtonsoft.Json*" }
        if ($existingJsonRef) {
            $existingJsonRef.ParentNode.RemoveChild($existingJsonRef)
        }
        
        $existingConfigRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Microsoft.WindowsAzure.Configuration*" }
        if ($existingConfigRef) {
            $existingConfigRef.ParentNode.RemoveChild($existingConfigRef)
        }
        
        # Dodaj WindowsAzure.Storage referencu
        $newStorageRef = $xml.CreateElement("Reference")
        $newStorageRef.SetAttribute("Include", "Microsoft.WindowsAzure.Storage")
        $storageHintPath = $xml.CreateElement("HintPath")
        $storageHintPath.InnerText = "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll"
        $newStorageRef.AppendChild($storageHintPath)
        $referenceItemGroup.AppendChild($newStorageRef)
        Write-Host "  Dodao WindowsAzure.Storage"
        
        # Dodaj Newtonsoft.Json referencu
        $newJsonRef = $xml.CreateElement("Reference")
        $newJsonRef.SetAttribute("Include", "Newtonsoft.Json")
        $jsonHintPath = $xml.CreateElement("HintPath")
        $jsonHintPath.InnerText = "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll"
        $newJsonRef.AppendChild($jsonHintPath)
        $referenceItemGroup.AppendChild($newJsonRef)
        Write-Host "  Dodao Newtonsoft.Json"
        
        # Dodaj Microsoft.WindowsAzure.Configuration referencu (samo za Common)
        if ($project -eq "Common") {
            $newConfigRef = $xml.CreateElement("Reference")
            $newConfigRef.SetAttribute("Include", "Microsoft.WindowsAzure.Configuration")
            $configHintPath = $xml.CreateElement("HintPath")
            $configHintPath.InnerText = "packages\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll"
            $newConfigRef.AppendChild($configHintPath)
            $referenceItemGroup.AppendChild($newConfigRef)
            Write-Host "  Dodao Microsoft.WindowsAzure.Configuration"
        }
        
        # Dodaj System.ComponentModel.DataAnnotations referencu (samo za Common)
        if ($project -eq "Common") {
            $existingDataAnnotationsRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*System.ComponentModel.DataAnnotations*" }
            if (-not $existingDataAnnotationsRef) {
                $newDataAnnotationsRef = $xml.CreateElement("Reference")
                $newDataAnnotationsRef.SetAttribute("Include", "System.ComponentModel.DataAnnotations")
                $referenceItemGroup.AppendChild($newDataAnnotationsRef)
                Write-Host "  Dodao System.ComponentModel.DataAnnotations"
            }
        }
        
        # Sačuvaj promene
        $xml.Save($csprojFile)
        Write-Host "$project ažuriran"
    }
}

Write-Host "Svi projekti su ažurirani!"
Write-Host "Sada kopiram DLL-ove u bin direktorijume..."

# Kopiraj DLL-ove u bin direktorijume
foreach ($project in $projects) {
    $binDebugDir = "$project\bin\Debug"
    $binReleaseDir = "$project\bin\Release"
    
    # Kreiraj direktorijume ako ne postoje
    if (-not (Test-Path $binDebugDir)) {
        New-Item -ItemType Directory -Path $binDebugDir -Force | Out-Null
    }
    if (-not (Test-Path $binReleaseDir)) {
        New-Item -ItemType Directory -Path $binReleaseDir -Force | Out-Null
    }
    
    # Kopiraj DLL-ove
    Copy-Item "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll" $binDebugDir -Force
    Copy-Item "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll" $binReleaseDir -Force
    Copy-Item "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" $binDebugDir -Force
    Copy-Item "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" $binReleaseDir -Force
    Copy-Item "packages\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll" $binDebugDir -Force
    Copy-Item "packages\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll" $binReleaseDir -Force
    
    Write-Host "Kopirani DLL-ovi u $project"
}

Write-Host "Svi DLL-ovi su kopirani!"
Write-Host "Sada zatvorite Visual Studio i otvorite ponovo CloudProjekt.sln"
Write-Host "Zatim izvršite Build -> Rebuild Solution"
