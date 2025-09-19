# SMTP Konfiguracija za Movie Discussion Forum

## Pregled
SMTP funkcionalnost je potpuno implementirana u NotificationService i koristi se za slanje email notifikacija korisnicima kada se doda novi komentar na diskusiju.

## Implementirane komponente

### 1. SMTPEmailSender klasa
- **Lokacija**: `NotificationService/SMTPEmailSender.cs`
- **Funkcionalnost**: 
  - Implementira `IEmailSender` interfejs
  - Podržava slanje emailova na više primalaca
  - Koristi SSL enkripciju
  - Ima timeout od 30 sekundi
  - Uključuje error handling

### 2. WorkerRole konfiguracija
- **Lokacija**: `NotificationService/WorkerRole.cs`
- **Funkcionalnost**:
  - Čita SMTP konfiguraciju iz ServiceConfiguration
  - Automatski kreira potrebne Azure tabele i redove
  - Obrađuje poruke iz reda "notifications"

### 3. Konfiguracija
- **ServiceConfiguration.Local.cscfg**: Konfiguracija za lokalno testiranje
- **ServiceConfiguration.Cloud.cscfg**: Konfiguracija za Azure deployment

## Konfiguracija SMTP

### Za Gmail (preporučeno)
1. Uključite 2-Factor Authentication na vašem Gmail nalogu
2. Generišite App Password:
   - Idite na Google Account Settings
   - Security → 2-Step Verification → App passwords
   - Generišite password za "Mail"
3. Ažurirajte konfiguraciju:

```xml
<Setting name="SmtpHost" value="smtp.gmail.com" />
<Setting name="SmtpPort" value="587" />
<Setting name="SmtpUser" value="your-email@gmail.com" />
<Setting name="SmtpPass" value="your-app-password" />
<Setting name="FromEmail" value="your-email@gmail.com" />
```

### Za Outlook/Hotmail
```xml
<Setting name="SmtpHost" value="smtp-mail.outlook.com" />
<Setting name="SmtpPort" value="587" />
<Setting name="SmtpUser" value="your-email@outlook.com" />
<Setting name="SmtpPass" value="your-password" />
<Setting name="FromEmail" value="your-email@outlook.com" />
```

### Za custom SMTP server
```xml
<Setting name="SmtpHost" value="your-smtp-server.com" />
<Setting name="SmtpPort" value="587" />
<Setting name="SmtpUser" value="your-username" />
<Setting name="SmtpPass" value="your-password" />
<Setting name="FromEmail" value="noreply@yourdomain.com" />
```

## Testiranje SMTP funkcionalnosti

### 1. Lokalno testiranje
1. Ažurirajte `ServiceConfiguration.Local.cscfg` sa vašim SMTP kredencijalima
2. Pokrenite NotificationService
3. Dodajte komentar u diskusiju
4. Proverite da li su emailovi poslati

### 2. Azure deployment
1. Ažurirajte `ServiceConfiguration.Cloud.cscfg` sa vašim SMTP kredencijalima
2. Deploy-ujte aplikaciju na Azure
3. Testirajte funkcionalnost

## Troubleshooting

### Česti problemi
1. **"Authentication failed"** - Proverite username/password
2. **"Connection timeout"** - Proverite SMTP host i port
3. **"SSL/TLS error"** - Proverite da li server podržava SSL na portu 587

### Logovi
NotificationService loguje sve SMTP operacije. Proverite Azure logs za detalje o greškama.

## Sigurnosne napomene
- Nikad ne commit-ujte prave SMTP kredencijale u git
- Koristite App Passwords umesto glavne lozinke
- Razmotrite korišćenje Azure Key Vault za production kredencijale
