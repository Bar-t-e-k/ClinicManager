# 🏥 ClinicManager 2.0 - System zarządzania przychodnią medyczną

Projekt zaliczeniowy zrealizowany w architekturze ASP.NET Core 10 (MVC). Aplikacja służy do kompleksowej obsługi przychodni medycznej, zarządzania pacjentami, wizytami, lekami oraz generowania raportów.

## 🧑‍🤝‍🧑 Zespół
- Bartłomiej Zięcina
- Krystian Strzępek

## 🚀 Technologie i Narzędzia
- **Backend:** .NET 10, ASP.NET Core MVC
- **Baza danych:** SQL Server, Entity Framework Core (Code-First)
- **Architektura / Wzorce:** Dependency Injection, Mapperly (DTOs)
- **Bezpieczeństwo:** ASP.NET Core Identity
- **Dokumentacja API:** OpenAPI (Swagger)
- **Raportowanie & Logowanie:** NLog, BackgroundTasks, generowanie PDF
- **Testy & CI/CD:** xUnit, NBomber, GitHub Actions

## 📋 Wymagania:
* **.NET 10 SDK** (lub nowszy)
* **Docker Desktop** (do bazy danych)
* **EF Core Tools** (`dotnet tool install --global dotnet-ef`)

## 📂 Struktura katalogów

- `src/ClinicManager.Web` – główny projekt webowy (MVC, Kontrolery, Widoki).
- `src/ClinicManager.Core` – (opcjonalnie, jeśli wydzielisz) logika biznesowa i encje.
- `tests/` – testy jednostkowe i wydajnościowe (NBomber).
- `docs/` – raporty PDF z analizy indeksów i logów SQL Profiler.

## ⚙️ Uruchomienie lokalne

1. Sklonuj repozytorium: `git clone https://github.com/TwojLogin/ClinicManager.git`
2. Skonfiguruj hasła i ConnectionString w User Secrets (patrz sekcja poniżej).
3. Wykonaj migracje bazy danych: 
   ```bash
   dotnet ef database update --project src/ClinicManager.Web --startup-project src/ClinicManager.Web
   ```
4. Uruchom aplikację: 
   ```bash
   dotnet run --project src/ClinicManager.Web
   ```

## ⚙️ CI/CD (GitHub Actions)

Projekt wykorzystuje automatyczny pipeline CI/CD skonfigurowany w GitHub Actions (`.github/workflows/dotnet-ci.yml`). 
Pipeline uruchamia się przy każdym wypchnięciu kodu (Push) oraz Pull Request na gałęzie `main` i `master`.

**Etapy workflow:**
1. **Checkout kodu** – pobranie najnowszej wersji z repozytorium.
2. **Setup .NET 10** – przygotowanie środowiska uruchomieniowego.
3. **Restore** – przywrócenie zależności projektu (NuGet).
4. **Build** – kompilacja aplikacji w trybie `Release`.
5. **Test** – automatyczne uruchomienie testów jednostkowych (xUnit/NUnit).

## 🐳 Uruchomienie lokalnej bazy danych (Docker)

Projekt wykorzystuje MS SQL Server uruchamiany w kontenerze Docker. 
Aby postawić bazę danych lokalnie, upewnij się, że masz zainstalowanego [Docker Desktop](https://www.docker.com/products/docker-desktop/), a następnie uruchom poniższą komendę w terminalu:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=ClinicAdmin!2026" -p 1433:1433 --name clinic-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

## 🔐 Konfiguracja ConnectionString i Seed Data

Aplikacja nie przechowuje haseł w plikach konfiguracyjnych. Skonfiguruj własne wpisy lokalnie:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ClinicManagerDb;User Id=sa;Password=ClinicAdmin!2026;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:AdminPassword" "Admin123!" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:DoctorPassword" "Lekarz123!" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:RegPassword" "Rej123!" --project src/ClinicManager.Web
```

## 🔐 Dane do logowania (Seed Data)

Aplikacja tworzy konta testowe przy pierwszym uruchomieniu (hasła muszą być ustawione w User Secrets – patrz wyżej):

| Rola | Login | Hasło (domyślne) |
|------|-------|------------------|
| Admin | `admin@clinic.com` | `Admin123!` |
| Lekarz | `lekarz@clinic.com` | `Lekarz123!` |
| Rejestratorka | `rejestracja@clinic.com` | `Rej123!` |

**Dostępne role w systemie:** `Admin`, `Lekarz`, `Rejestratorka`.

## 📧 Raport nadchodzących wizyt (US#7)

Usługa w tle (`UpcomingVisitsReportBackgroundService`) generuje PDF z wizytami na **jutro** i wysyła go e-mailem (SMTP / MailKit). Plik: `src/ClinicManager.Web/reports/raport-nadchodzace-wizyty.pdf`.

Konfiguracja: sekcja `UpcomingVisitsReport` w `appsettings.json`, nadpisanie przez User Secrets.

**Mailtrap:** użyj zakładki **Email Testing → Inbox → SMTP Settings** (host, port, username, password). Nie używaj API tokena z produktu *Email Sending* – aplikacja łączy się przez SMTP.

Przed testem dodaj w systemie **2–3 wizyty na jutro** (status: Zaplanowana lub Potwierdzona).

```bash
dotnet user-secrets set "UpcomingVisitsReport:IntervalMinutes" "2" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:AdminEmail" "twoj@email.pl" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Host" "sandbox.smtp.mailtrap.io" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Port" "587" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:UseSsl" "true" --project src/ClinicManager.Web
```

Następnie skopiuj z Mailtrap pola **Username** i **Password** (przycisk *Show password*) i ustaw je w osobnych komendach – w cudzysłowie wklejasz dokładnie to, co widzisz w panelu:

```bash
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Username" "TWOJ_USERNAME_Z_MAILTRAP" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Password" "TWOJE_HASLO_Z_MAILTRAP" --project src/ClinicManager.Web
```

Po `dotnet run` (domyślnie co 2 min w tej konfiguracji) sprawdź folder `reports/` oraz skrzynkę Mailtrap. Na produkcji ustaw `IntervalMinutes` na `1440`.

## 📈 Testy wydajności NBomber (US#8)

Endpoint API pod obciążenie: **GET `/api/visits/active`** – aktywne wizyty z danymi pacjenta i lekarza (zapytanie z JOIN-ami). Dokumentacja w Swaggerze (Development): `/swagger`.

Kod testu: `tests/ClinicManager.PerformanceTests/VisitsLoadTest.cs`  
Scenariusz: **50** równoległych użytkowników + **100** żądań.

**Uruchomienie** (wymaga działającej aplikacji na `http://localhost:5215`):

```bash
# Terminal 1
dotnet run --project src/ClinicManager.Web

# Terminal 2 (z katalogu repozytorium)
dotnet run --project tests/ClinicManager.PerformanceTests
```

Wyniki:
- `nbomber-report.pdf` – raport PDF (czasy odpowiedzi, RPS, błędy)
- folder `nbomber-report/` – raport HTML/TXT z NBomber

Test wydajnościowy **nie** jest uruchamiany przez `dotnet test` (osobny projekt konsolowy).

## ❓ Rozwiązywanie problemów
* **Błąd logowania SA w Dockerze:** Upewnij się, że kontener `clinic-sql` działa (`docker ps`). Jeśli zmieniłeś hasło w komendzie `docker run`, musisz je również zaktualizować w `user-secrets`.
* **Błąd migracji:** Jeśli `dotnet ef database update` nie działa, upewnij się, że jesteś w głównym folderze projektu i masz zainstalowane narzędzia `dotnet-ef`.
* **Port 5215 zajęty:** Zatrzymaj poprzedni `dotnet run` (Ctrl+C) lub zamknij proces `ClinicManager.Web.exe`.
* **Raport e-mail:** SMTP z Mailtrap Email Testing (nie API Sending). Wizyty muszą być zaplanowane na **jutro**. Błąd autoryzacji SMTP – sprawdź Username i Password w user-secrets.
* **NBomber – brak połączenia:** Najpierw uruchom `ClinicManager.Web`, potem projekt `ClinicManager.PerformanceTests`. Sprawdź endpoint w przeglądarce: `/api/visits/active`.