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
```
## 🔐 Dane do logowania (Seed Data)
Aplikacja automatycznie konfiguruje system Identity i tworzy domyślnych użytkowników przy pierwszym uruchomieniu w środowisku Development:

| Rola | Login | Hasło |
|------|-------|-------|
| Admin | `admin@clinic.com` | `Admin123!` |
| Lekarz (testowy) | `lekarz@clinic.com` | `Lekarz123!` |

**Dostępne role w systemie:** `Admin`, `Lekarz`, `Rejestratorka`.
## ❓ Rozwiązywanie problemów
* **Błąd logowania SA w Dockerze:** Upewnij się, że kontener `clinic-sql` działa (`docker ps`). Jeśli zmieniłeś hasło w komendzie `docker run`, musisz je również zaktualizować w `user-secrets`.
* **Błąd migracji:** Jeśli `dotnet ef database update` nie działa, upewnij się, że jesteś w głównym folderze projektu i masz zainstalowane narzędzia `dotnet-ef`.
* **Docker Desktop nie odpowiada:** Uruchom Docker Desktop ręcznie z menu Start i poczekaj aż ikona wieloryba w zasobniku przestanie się animować.
