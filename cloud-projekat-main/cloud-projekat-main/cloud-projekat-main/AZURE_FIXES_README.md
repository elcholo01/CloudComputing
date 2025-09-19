# Popravka Azure Storage Grešaka

## Problem
Projekat je imao greške vezane za Azure Storage reference i using direktive:
- `The name 'CloudStorageAccount' does not exist in the current context`
- `The type or namespace name 'CloudTable' could not be found`
- `The type or namespace name 'TableEntity' could not be found`
- `The type or namespace name 'WindowsAzure' does not exist in the namespace 'Microsoft'`

## Rešenje
Kreirane su PowerShell skripte koje su automatski dodale sve potrebne reference i using direktive:

### 1. FixAzureReferences.ps1
- Dodaje potrebne reference u .csproj fajlove:
  - `Microsoft.WindowsAzure.Storage`
  - `Microsoft.WindowsAzure.Configuration`
  - `Newtonsoft.Json`
  - `System.ComponentModel.DataAnnotations`
- Dodaje potrebne using direktive u .cs fajlove:
  - `using Microsoft.WindowsAzure.Storage;`
  - `using Microsoft.WindowsAzure.Storage.Table;`
  - `using Microsoft.WindowsAzure.Storage.Queue;` (ako se koristi CloudQueue)

### 2. VerifyNuGetPackages.ps1
- Proverava da li su svi potrebni NuGet paketi instalirani:
  - `WindowsAzure.Storage` (9.3.3)
  - `Newtonsoft.Json` (13.0.3)
  - `Microsoft.WindowsAzure.ConfigurationManager` (3.2.3)

### 3. FinalCheck.ps1
- Proverava da li su sve greške rešene
- Daje rezime o stanju projekta

## Koraci za primenu popravki

1. **Pokrenite skripte:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File "FixAzureReferences.ps1"
   powershell -ExecutionPolicy Bypass -File "VerifyNuGetPackages.ps1"
   powershell -ExecutionPolicy Bypass -File "FinalCheck.ps1"
   ```

2. **U Visual Studio-u:**
   - Zatvorite Visual Studio
   - Otvorite ponovo `CloudProjekt.sln`
   - Uradite **Clean Solution** (Build → Clean Solution)
   - Uradite **Rebuild Solution** (Build → Rebuild Solution)

3. **Ako i dalje ima grešaka:**
   - Proverite NuGet pakete u Package Manager-u
   - Instalirajte nedostajuće pakete ako je potrebno

## Projekti koji su popravljeni
- ✅ Common
- ✅ MovieDiscussionService
- ✅ NotificationService
- ✅ HealthMonitoringService
- ✅ AdminToolsConsoleApp
- ✅ HealthStatusService

## Status
Sve Azure Storage greške su rešene! Projekat bi trebalo da se može uspešno build-ovati.
