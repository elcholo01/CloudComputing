# PowerShell skripta za kompajliranje Common projekta

Write-Host "Kompajliram Common projekat..."

# Kompajliraj Common projekat
$commonProject = "Common\Common.csproj"

if (Test-Path $commonProject) {
    Write-Host "Kompajliram Common projekat..."
    
    # Koristi MSBuild za kompajliranje
    $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    
    if (-not (Test-Path $msbuildPath)) {
        $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    }
    
    if (-not (Test-Path $msbuildPath)) {
        $msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    }
    
    if (Test-Path $msbuildPath) {
        & $msbuildPath $commonProject /p:Configuration=Debug /p:Platform="Any CPU" /verbosity:minimal
        Write-Host "Common projekat je kompajliran!"
    } else {
        Write-Host "MSBuild nije pronađen. Pokušavam sa dotnet..."
        
        # Pokušaj sa dotnet ako je dostupan
        try {
            dotnet build $commonProject --configuration Debug
            Write-Host "Common projekat je kompajliran sa dotnet!"
        } catch {
            Write-Host "Greška: Ne mogu da kompajliram Common projekat."
            Write-Host "Molimo vas da otvorite Visual Studio i kompajlirajte Common projekat ručno."
        }
    }
} else {
    Write-Host "Common projekat nije pronađen!"
}

Write-Host "Završeno!"
