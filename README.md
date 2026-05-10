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

## ⚙️ Uruchomienie lokalne

1. Sklonuj repozytorium: `git clone https://github.com/TwojLogin/ClinicManager.git`
2. Zaktualizuj ConnectionString w `appsettings.json` (wskazując na swój lokalny SQL Server).
3. Wykonaj migracje bazy danych: 
   ```bash
   dotnet ef database update --project src/ClinicManager.Web