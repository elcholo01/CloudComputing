# PowerShell skripta za automatsko dodavanje reference-a

$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    
    if (Test-Path $csprojFile) {
        Write-Host "Ažuriram $project..."
        
        # Učitaj XML fajl
        [xml]$xml = Get-Content $csprojFile
        
        # Dodaj WindowsAzure.Storage referencu ako ne postoji
        $storageRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*WindowsAzure.Storage*" }
        if (-not $storageRef) {
            $itemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Microsoft.WindowsAzure.Storage")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "..\packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll"
            $newRef.AppendChild($hintPath)
            $itemGroup.AppendChild($newRef)
        }
        
        # Dodaj Newtonsoft.Json referencu ako ne postoji
        $jsonRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Newtonsoft.Json*" }
        if (-not $jsonRef) {
            $itemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Newtonsoft.Json")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll"
            $newRef.AppendChild($hintPath)
            $itemGroup.AppendChild($newRef)
        }
        
        # Dodaj Microsoft.WindowsAzure.Configuration referencu ako ne postoji
        $configRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Microsoft.WindowsAzure.Configuration*" }
        if (-not $configRef) {
            $itemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Microsoft.WindowsAzure.Configuration")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "..\packages\Microsoft.WindowsAzure.ConfigurationManager.3.2.3\lib\net40\Microsoft.WindowsAzure.Configuration.dll"
            $newRef.AppendChild($hintPath)
            $itemGroup.AppendChild($newRef)
        }
        
        # Sačuvaj promene
        $xml.Save($csprojFile)
        Write-Host "✓ $project ažuriran"
    }
}

Write-Host "Svi projekti su ažurirani!"
