# PowerShell skripta za rešavanje preostalih problema

Write-Host "Rešavam preostale probleme..."

# 1. Dodaj Microsoft.WindowsAzure.ServiceRuntime reference u sve projekte
$projects = @("Common", "MovieDiscussionService", "NotificationService", "HealthMonitoringService", "AdminToolsConsoleApp", "HealthStatusService")

foreach ($project in $projects) {
    $csprojFile = "$project\$project.csproj"
    
    if (Test-Path $csprojFile) {
        Write-Host "Dodajem ServiceRuntime u $project..."
        
        [xml]$xml = Get-Content $csprojFile
        
        $referenceItemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
        if (-not $referenceItemGroup) {
            $referenceItemGroup = $xml.CreateElement("ItemGroup")
            $xml.Project.AppendChild($referenceItemGroup)
        }
        
        # Dodaj Microsoft.WindowsAzure.ServiceRuntime
        $serviceRuntimeRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*Microsoft.WindowsAzure.ServiceRuntime*" }
        if (-not $serviceRuntimeRef) {
            $newRef = $xml.CreateElement("Reference")
            $newRef.SetAttribute("Include", "Microsoft.WindowsAzure.ServiceRuntime")
            $hintPath = $xml.CreateElement("HintPath")
            $hintPath.InnerText = "packages\Microsoft.WindowsAzure.ServiceRuntime.2.7.0\lib\net40\Microsoft.WindowsAzure.ServiceRuntime.dll"
            $newRef.AppendChild($hintPath)
            $referenceItemGroup.AppendChild($newRef)
            Write-Host "  Dodao Microsoft.WindowsAzure.ServiceRuntime"
        }
        
        $xml.Save($csprojFile)
    }
}

# 2. Dodaj System.Web.Mvc reference u HealthStatusService
Write-Host "Dodajem System.Web.Mvc u HealthStatusService..."
$healthStatusCsproj = "HealthStatusService\HealthStatusService.csproj"
if (Test-Path $healthStatusCsproj) {
    [xml]$xml = Get-Content $healthStatusCsproj
    
    $referenceItemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
    if (-not $referenceItemGroup) {
        $referenceItemGroup = $xml.CreateElement("ItemGroup")
        $xml.Project.AppendChild($referenceItemGroup)
    }
    
    # Dodaj System.Web.Mvc
    $mvcRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*System.Web.Mvc*" }
    if (-not $mvcRef) {
        $newRef = $xml.CreateElement("Reference")
        $newRef.SetAttribute("Include", "System.Web.Mvc")
        $hintPath = $xml.CreateElement("HintPath")
        $hintPath.InnerText = "packages\Microsoft.AspNet.Mvc.5.2.9\lib\net45\System.Web.Mvc.dll"
        $newRef.AppendChild($hintPath)
        $referenceItemGroup.AppendChild($newRef)
        Write-Host "  Dodao System.Web.Mvc"
    }
    
    # Dodaj System.Web.Optimization
    $optimizationRef = $xml.Project.ItemGroup.Reference | Where-Object { $_.Include -like "*System.Web.Optimization*" }
    if (-not $optimizationRef) {
        $newRef = $xml.CreateElement("Reference")
        $newRef.SetAttribute("Include", "System.Web.Optimization")
        $hintPath = $xml.CreateElement("HintPath")
        $hintPath.InnerText = "packages\Microsoft.AspNet.Web.Optimization.1.1.3\lib\net40\System.Web.Optimization.dll"
        $newRef.AppendChild($hintPath)
        $referenceItemGroup.AppendChild($newRef)
        Write-Host "  Dodao System.Web.Optimization"
    }
    
    $xml.Save($healthStatusCsproj)
}

# 3. Kopiraj DLL-ove u bin direktorijume
Write-Host "Kopiram DLL-ove..."
foreach ($project in $projects) {
    $binDebugDir = "$project\bin\Debug"
    $binReleaseDir = "$project\bin\Release"
    
    if (-not (Test-Path $binDebugDir)) {
        New-Item -ItemType Directory -Path $binDebugDir -Force | Out-Null
    }
    if (-not (Test-Path $binReleaseDir)) {
        New-Item -ItemType Directory -Path $binReleaseDir -Force | Out-Null
    }
    
    # Kopiraj potrebne DLL-ove
    $dlls = @(
        "packages\WindowsAzure.Storage.9.3.3\lib\net45\Microsoft.WindowsAzure.Storage.dll",
        "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll",
        "packages\Microsoft.WindowsAzure.ServiceRuntime.2.7.0\lib\net40\Microsoft.WindowsAzure.ServiceRuntime.dll"
    )
    
    foreach ($dll in $dlls) {
        if (Test-Path $dll) {
            Copy-Item $dll $binDebugDir -Force
            Copy-Item $dll $binReleaseDir -Force
        }
    }
    
    Write-Host "Kopirani DLL-ovi u $project"
}

# 4. Dodaj Microsoft.WindowsAzure.Configuration u Common
Write-Host "Dodajem Microsoft.WindowsAzure.Configuration u Common..."
$commonCsproj = "Common\Common.csproj"
if (Test-Path $commonCsproj) {
    [xml]$xml = Get-Content $commonCsproj
    
    $referenceItemGroup = $xml.Project.ItemGroup | Where-Object { $_.Reference } | Select-Object -First 1
    if (-not $referenceItemGroup) {
        $referenceItemGroup = $xml.CreateElement("ItemGroup")
        $xml.Project.AppendChild($referenceItemGroup)
    }
    
    # Dodaj Microsoft.WindowsAzure.Configuration
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
    
    $xml.Save($commonCsproj)
}

Write-Host "Svi problemi su rešeni!"
Write-Host "Sada zatvorite Visual Studio i otvorite ponovo CloudProjekt.sln"
Write-Host "Zatim izvršite Build -> Rebuild Solution"
