# PowerShell skripta za direktno dodavanje reference-a u sve projekte

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
        
        # Dodaj Microsoft.WindowsAzure.Configuration referencu (samo za Common)
        if ($project -eq "Common") {
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
        }
        
        # Dodaj System.ComponentModel.DataAnnotations referencu (samo za Common)
        if ($project -eq "Common") {
            $dataAnnotationsRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*System.ComponentModel.DataAnnotations*" }
            if (-not $dataAnnotationsRef) {
                $newRef = $xml.CreateElement("Reference")
                $newRef.SetAttribute("Include", "System.ComponentModel.DataAnnotations")
                $referenceItemGroup.AppendChild($newRef)
                Write-Host "  Dodao System.ComponentModel.DataAnnotations"
            }
        }
        
        # Sačuvaj promene
        $xml.Save($csprojFile)
        Write-Host "$project ažuriran"
    }
}

Write-Host "Svi projekti su ažurirani!"
Write-Host "Sada zatvorite Visual Studio i otvorite ponovo CloudProjekt.sln"
