# 🏥 ClinicManager 2.0 - System zarządzania przychodnią medyczną

Projekt zaliczeniowy zrealizowany w architekturze ASP.NET Core 10 (MVC). 
Aplikacja służy do kompleksowej obsługi przychodni medycznej, zarządzania pacjentami, wizytami, lekami, a także generowania raportów i dbania o nienaruszalność dokumentacji medycznej.

## 🧑‍🤝‍🧑 Zespół
- Bartłomiej Zięcina
- Krystian Strzępek

## 🚀 Technologie i Narzędzia
- **Backend:** .NET 10, ASP.NET Core MVC
- **Baza danych:** SQL Server, Entity Framework Core (Code-First)
- **Architektura / Wzorce:** Dependency Injection, Mapperly (DTOs), Repository Pattern (Services)
- **Bezpieczeństwo:** ASP.NET Core Identity (RBAC - Role-Based Access Control)
- **Dokumentacja API:** OpenAPI (Swagger)
- **Raportowanie & Logowanie:** NLog, BackgroundTasks, generowanie PDF
- **Testy & CI/CD:** xUnit, NBomber, GitHub Actions

## 🌟 Kluczowe funkcjonalności i architektura
Poza standardowym systemem CRUD, projekt wdraża zaawansowane reguły biznesowe chroniące dane medyczne:
- **Soft Delete i Archiwum Pacjentów:** Zamiast trwałego usuwania z bazy, pacjenci są "miękko" usuwani (ukrywani) wraz z nałożeniem blokady logowania. Administrator posiada dostęp do dedykowanego Archiwum, z którego może w dowolnej chwili **reaktywować** konto pacjenta.
- **Nienaruszalność Historii Lekarskiej:** Kont personelu medycznego nie da się usunąć. Administrator może jedynie **zablokować (Lockout)** lekarza (pod warunkiem braku aktywnych wizyt), co gwarantuje spójność historycznych raportów medycznych i zachowanie audytu (kto leczył pacjenta).
- **Zabezpieczenie Edycji Wizyt:** Wizyty o statusie "Zakończona" i "Odwołana" są zablokowane do edycji, chroniąc integralność historii leczenia.
- **Panel Ustawień:** Zalogowani użytkownicy mogą bezpiecznie zmieniać swoje hasła.

## 📋 Wymagania:
* **.NET 10 SDK** (lub nowszy)
* **Docker Desktop** (do bazy danych)
* **EF Core Tools** (`dotnet tool install --global dotnet-ef`)

## 📂 Struktura katalogów
- `src/ClinicManager.Web` – główny projekt webowy (MVC, Kontrolery, Serwisy, Widoki).
- `tests/ClinicManager.Tests` – testy jednostkowe (xUnit, Moq) weryfikujące poprawność serwisów i kontrolerów.
- `tests/ClinicManager.PerformanceTests` – testy wydajnościowe (NBomber).
- `docs/` – screeny z testów i wykorzystane pliki SQL.

## 🐳 Uruchomienie lokalnej bazy danych (Docker)
Projekt wykorzystuje MS SQL Server uruchamiany w kontenerze Docker. 
Aby postawić bazę danych lokalnie, upewnij się, że masz zainstalowanego [Docker Desktop](https://www.docker.com/products/docker-desktop/), a następnie uruchom poniższą komendę w terminalu:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=ClinicAdmin!2026" -p 1433:1433 --name clinic-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Oczywiście nazwę kontenera, hasło i port możesz dostosować do swoich potrzeb, ale pamiętaj, aby zaktualizować te dane również w `user-secrets` (patrz sekcja poniżej).

## 🔐 Konfiguracja ConnectionString i Seed Data
Aplikacja nie przechowuje haseł w plikach konfiguracyjnych. Skonfiguruj własne wpisy lokalnie w User Secrets:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ClinicManagerDb;User Id=sa;Password=ClinicAdmin!2026;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:AdminPassword" "Admin123!" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:DoctorPassword" "Lekarz123!" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:RegPassword" "Rej123!" --project src/ClinicManager.Web
```

## ⚙️ Uruchomienie lokalne
1. Sklonuj repozytorium: `git clone https://github.com/Bar-t-e-k/ClinicManager.git`
2. Skonfiguruj hasła i ConnectionString w User Secrets.
3. Wykonaj migracje bazy danych: 
```bash
   dotnet ef database update --project src/ClinicManager.Web --startup-project src/ClinicManager.Web
```
4. Uruchom aplikację: 
```bash
   dotnet run --project src/ClinicManager.Web
```
5. Wpisz następujący adres w przeglądarce: 
```bash
   http://localhost:5215
```

## 🔐 Dane do logowania (Seed Data)
Aplikacja automatycznie konfiguruje system Identity i tworzy domyślnych użytkowników przy pierwszym uruchomieniu w środowisku:

Aplikacja tworzy konta testowe przy pierwszym uruchomieniu (hasła muszą być ustawione w User Secrets – patrz wyżej):

| Rola | Login | Hasło (domyślne) |
|------|-------|------------------|
| Admin | `admin@clinic.com` | `Admin123!` |
| Lekarz | `lekarz@clinic.com` | `Lekarz123!` |
| Rejestratorka | `rejestracja@clinic.com` | `Rej123!` |

**Dostępne role w systemie:** `Admin`, `Lekarz`, `Rejestratorka`, `Pacjent`.

## ⚙️ CI/CD (GitHub Actions)
Projekt wykorzystuje automatyczny pipeline CI/CD skonfigurowany w GitHub Actions (`.github/workflows/dotnet-ci.yml`). 
Pipeline uruchamia się przy każdym wypchnięciu kodu (Push) oraz Pull Request na gałęzie `main` i `master`.

**Etapy workflow:**
1. **Checkout kodu** – pobranie najnowszej wersji.
2. **Setup .NET 10** – przygotowanie środowiska uruchomieniowego.
3. **Restore** – przywrócenie zależności projektu (NuGet).
4. **Build** – kompilacja aplikacji w trybie `Release`.
5. **Test** – automatyczne uruchomienie testów jednostkowych (xUnit).

## 📧 Raport nadchodzących wizyt

Usługa w tle (`UpcomingVisitsReportBackgroundService`) generuje PDF z wizytami na **jutro** i wysyła go e-mailem (SMTP / MailKit). Plik zapisywany jest również do: `src/ClinicManager.Web/reports/raport-nadchodzace-wizyty.pdf`.

Konfiguracja: sekcja `UpcomingVisitsReport` w `appsettings.json`, nadpisanie przez User Secrets (np. dla Mailtrap).

Przed testem dodaj w systemie 2–3 wizyty na jutro (status: Zaplanowana lub Potwierdzona).

```bash
dotnet user-secrets set "UpcomingVisitsReport:IntervalMinutes" "2" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:AdminEmail" "twoj@email.pl" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Host" "sandbox.smtp.mailtrap.io" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Port" "587" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:UseSsl" "true" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Username" "TWOJ_USERNAME" --project src/ClinicManager.Web
dotnet user-secrets set "UpcomingVisitsReport:Smtp:Password" "TWOJE_HASLO" --project src/ClinicManager.Web
```

## 📈 Testy wydajności NBomber

Endpoint API pod obciążenie: **GET `/api/visits/active`** – aktywne wizyty z danymi pacjenta i lekarza (zapytanie z JOIN-ami). Dokumentacja w Swaggerze (Development): `/swagger`.

Scenariusz: **50** równoległych użytkowników + **100** żądań.

**Uruchomienie** (wymaga działającej aplikacji na `http://localhost:5215`):

```bash
# Terminal 1
dotnet run --project src/ClinicManager.Web

# Terminal 2 (z głównego katalogu repozytorium)
dotnet run --project tests/ClinicManager.PerformanceTests
```

Wyniki:
- `nbomber-report.pdf` – raport PDF (czasy odpowiedzi, RPS, błędy)
- folder `nbomber-report/` – raport HTML/TXT z NBomber

## 🗄️ Optymalizacja bazy danych – indeksy

Przyspieszenie wyszukiwania po **PESEL** (pacjenci) i **lekarzu** (wizyty).

Optymalizator zapytań SQL przeszedł z operacji Index Scan na wydajne Index Seek.

**Indeksy Non-Clustered (EF Core Fluent API)** – `ClinicDbContext.cs`:

| Indeks | Tabela | Kolumny | Filtr |
|--------|--------|---------|-------|
| `IX_Patients_Pesel` | Patients | Pesel (UNIQUE) | `[IsDeleted] = 0` |
| `IX_Visits_DoctorId_ScheduledDate` | Visits | DoctorId, ScheduledDate | `[IsDeleted] = 0` |

Screenshoty oraz pliki sql znajdują się w folderze docs/.

## ❓ Rozwiązywanie problemów
* **Błąd logowania SA w Dockerze**: Upewnij się, że kontener clinic-sql działa (docker ps). Jeśli zmieniłeś hasło w komendzie docker run, musisz je również zaktualizować w user-secrets.

* **Błąd migracji**: Jeśli dotnet ef database update nie działa, upewnij się, że jesteś w głównym folderze projektu i masz zainstalowane narzędzia dotnet-ef.

* **Port 5215 zajęty**: Zatrzymaj poprzedni proces dotnet run (Ctrl+C) lub zamknij proces ClinicManager.Web.exe.

* **Docker Desktop nie odpowiada**: Uruchom Docker Desktop ręcznie z menu Start i poczekaj aż ikona wieloryba w zasobniku przestanie się animować.

* **Brak usuniętych pacjentów na liście do reaktywacji**: Upewnij się, że korzystasz z IgnoreQueryFilters() podczas odpytywania EF Core.
