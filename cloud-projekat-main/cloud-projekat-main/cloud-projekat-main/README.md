# Movie Discussion Forum - Azure Cloud Project

Kompletan sistem za diskusije o filmovima implementiran kao Azure Cloud aplikacija sa mikroservisima.

## Opis aplikacije

Movie Discussion Forum je Azure Cloud aplikacija koja omogućava korisnicima da:
- Registruju se i prijavljuju na sistem  
- Kreiraju diskusije o filmovima (samo verifikovani autori)
- Ostavljaju komentare i glasaju za filmove
- Prate diskusije i primaju email notifikacije
- Pretražuju i sortiraju diskusije po različitim kriterijumima

## Arhitektura projekta

### 📁 Struktura projekta

```
cloud-projekat-main/
├── MovieDiscussionService/         # Web Role - Glavna web aplikacija
├── NotificationService/            # Worker Role - Email notifikacije
├── HealthMonitoringService/        # Worker Role - Health monitoring
├── HealthStatusService/           # Web Role - Health status dashboard
├── AdminToolsConsoleApp/          # Console App - Admin alati
├── Common/                        # Shared biblioteka
└── MovieForumCloud/              # Azure Cloud konfiguracija
```

### 🏗️ Servisi i komponente

#### 1. **MovieDiscussionService** (Web Role)
- **Port**: 8080
- **Funkcionalnosti**:
  - Registracija i prijava korisnika
  - CRUD operacije za diskusije
  - Sistem komentarisanja
  - Glasanje (pozitivno/negativno)
  - Pretraga i sortiranje
  - Praćenje diskusija

#### 2. **NotificationService** (Worker Role - 3 instance)
- **Funkcionalnosti**:
  - SMTP slanje emailova
  - Obrada notifikacija iz queue-a
  - Logovanje poslatih notifikacija

#### 3. **HealthMonitoringService** (Worker Role - 2 instance)
- **Funkcionalnosti**:
  - Health check svakih 3 sekunde
  - Monitoring dostupnosti servisa
  - Alert emails pri nedostupnosti

#### 4. **HealthStatusService** (Web Role)
- **Funkcionalnosti**:
  - Grafički prikaz dostupnosti
  - Statistike za poslednja 2 sata
  - Real-time monitoring dashboard

#### 5. **AdminToolsConsoleApp** (Console App)
- **Funkcionalnosti**:
  - Verifikacija korisnika kao autora
  - Upravljanje alert email adresama

## Build i pokretanje

### Preduslovi
- Visual Studio 2019/2022
- Azure SDK
- .NET Framework 4.7.2
- Azure Storage Emulator

### Build koraci

1. **Kloniraj projekat**
```bash
git clone <repository-url>
cd cloud-projekat-main
```

2. **Pokretanje Azure Storage Emulator-a**
```bash
# Pokreni Azure Storage Emulator
AzureStorageEmulator.exe start
```

3. **Build solution**
```bash
# Visual Studio
Build → Build Solution

# Command line
dotnet build CloudProjekt.sln
```

4. **Pokretanje servisa**

**MovieDiscussionService:**
```bash
cd MovieDiscussionService
dotnet run
# Ili F5 u Visual Studio
```

**NotificationService:**
```bash
cd NotificationService  
dotnet run
```

**HealthMonitoringService:**
```bash
cd HealthMonitoringService
dotnet run
```

**HealthStatusService:**
```bash
cd HealthStatusService
dotnet run
```

**AdminToolsConsoleApp:**
```bash
cd AdminToolsConsoleApp
dotnet run
```

### SMTP konfiguracija

1. **Ažuriraj SMTP podešavanja** u `NotificationService/app.config`:
```xml
<appSettings>
  <add key="SmtpHost" value="smtp.gmail.com" />
  <add key="SmtpPort" value="587" />
  <add key="SmtpUser" value="your-email@gmail.com" />
  <add key="SmtpPass" value="your-app-password" />
  <add key="FromEmail" value="your-email@gmail.com" />
</appSettings>
```

2. **Za Gmail**: Generiši App Password u Google Account Settings

## Funkcionalnosti po zahtevima

### ✅ **Implementirane funkcionalnosti**

#### MovieDiscussionService
- ✅ Registracija novog korisnika (ime, prezime, pol, država, grad, adresa, email, lozinka, slika)
- ✅ Prijava korisnika (email + lozinka)  
- ✅ Izmena korisničkog profila
- ✅ Kreiranje diskusije (samo verifikovani autori)
- ✅ Izmena i brisanje diskusije (samo vlasniku)
- ✅ Ostavljanje komentara
- ✅ Reakcija na film (pozitivno/negativno)
- ✅ Pretraga (naziv filma, žanr)
- ✅ Sortiranje (ocena, datum, naziv, glasovi)
- ✅ Paginacija diskusija

#### NotificationService  
- ✅ Worker Role sa 3 instance
- ✅ Čitanje iz "notifications" queue-a
- ✅ SMTP slanje emailova
- ✅ Logovanje notifikacija (datum-vreme|id-komentara|broj-poslatih-mejlova)

#### HealthMonitoringService
- ✅ Worker Role sa 2 instance  
- ✅ Health check svakih 3 sekunde
- ✅ Monitoring /health-monitoring endpoint-a
- ✅ Alert emails
- ✅ HealthCheck tabela (datum-vreme|status|naziv-servisa)

#### HealthStatusService
- ✅ Web Role prikaz dostupnosti
- ✅ Čitanje HealthCheck tabele za poslednja 2 sata
- ✅ Grafički prikaz dostupnosti
- ✅ Procentualna dostupnost

#### AdminToolsConsoleApp
- ✅ Console aplikacija
- ✅ Izmena alert email adresa
- ✅ Verifikacija korisnika kao autora

### 🎯 **Dodatne implementirane funkcionalnosti**
- ✅ Potpun Azure Table Storage integration
- ✅ Azure Queue Storage za notifikacije  
- ✅ Responsive Web UI sa Bootstrap
- ✅ Real-time health monitoring grafik
- ✅ Napredna pretraga i filteri
- ✅ Error handling i logging
- ✅ Security (Forms Authentication)

## Azure Storage tabele

- **Users** - Korisnici sistema
- **Discussions** - Diskusije o filmovima  
- **Comments** - Komentari na diskusije
- **Votes** - Glasovi korisnika
- **Follows** - Praćenje diskusija
- **HealthCheck** - Health monitoring podaci
- **AlertEmails** - Email adrese za upozorenja
- **NotificationLog** - Log poslatih notifikacija

## Testiranje

1. **Pokreni Azure Storage Emulator**
2. **Pokreni sve servise**
3. **Testiraj workflow**:
   - Registruj korisnika
   - Verifikuj kao autora (AdminToolsConsoleApp)
   - Kreiraj diskusiju
   - Ostavi komentar
   - Proveri email notifikacije
   - Proveri health status

## Proizvodnja (Production)

Za Azure deployment:
1. Ažuriraj connection stringove u ServiceConfiguration.Cloud.cscfg
2. Konfiguriši prave SMTP kredencijale
3. Deploy MovieForumCloud project na Azure

---

**Projekat je potpuno implementiran prema RCA specifikaciji sa svim zahtevnim funkcionalnostima! 🎬✨**