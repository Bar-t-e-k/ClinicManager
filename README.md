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

## 📂 Struktura katalogów

- `src/ClinicManager.Web` – główny projekt webowy (MVC, Kontrolery, Widoki).
- `src/ClinicManager.Core` – (opcjonalnie, jeśli wydzielisz) logika biznesowa i encje.
- `tests/` – testy jednostkowe i wydajnościowe (NBomber).
- `docs/` – raporty PDF z analizy indeksów i logów SQL Profiler.

## ⚙️ Uruchomienie lokalne

1. Sklonuj repozytorium: `git clone https://github.com/TwojLogin/ClinicManager.git`
2. Zaktualizuj ConnectionString w `appsettings.json` (wskazując na swój lokalny SQL Server).
3. Wykonaj migracje bazy danych: 
   ```bash
   dotnet ef database update --project src/ClinicManager.Web --startup-project src/ClinicManager.Web
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

## 🔐 Dane do logowania (Seed Data)

Aplikacja automatycznie konfiguruje system Identity i tworzy domyślnego administratora przy pierwszym uruchomieniu:

- **Login:** `admin@clinic.com`
- **Hasło:** `Admin123!`

**Dostępne role w systemie:** `Admin`, `Lekarz`, `Rejestratorka`.