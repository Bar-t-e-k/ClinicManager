# US#9 – Optymalizacja bazy danych (indeksy)

Przyspieszenie wyszukiwania po **PESEL** i **lekarzu** w systemie ClinicManager.

## Cel

| Scenariusz | Serwis / metoda | Tabela |
|------------|-----------------|--------|
| Sprawdzenie unikalności PESEL | `PatientService.PeselExistsAsync` | `Patients` |
| Wyszukiwanie pacjenta po PESEL | `PatientService.GetAllPatientsAsync` | `Patients` |
| Lista wizyt lekarza | `VisitService.GetAllVisitsAsync` | `Visits` |

## Indeksy Non-Clustered (EF Core Fluent API)

Konfiguracja w `src/ClinicManager.Web/Data/ClinicDbContext.cs`:

```csharp
// PESEL – unikalny indeks filtrowany (tylko aktywni pacjenci)
modelBuilder.Entity<Patient>()
    .HasIndex(p => p.Pesel)
    .IsUnique()
    .HasFilter("[IsDeleted] = 0");

// Lekarz – composite index + filtr IsDeleted
modelBuilder.Entity<Visit>()
    .HasIndex(v => new { v.DoctorId, v.ScheduledDate })
    .HasFilter("[IsDeleted] = 0");
```

### Migracje EF Core

| Migracja | Indeks | Typ |
|----------|--------|-----|
| `20260530095912_AddUniquePeselIndex` | `IX_Patients_Pesel` | UNIQUE NONCLUSTERED, filter `[IsDeleted] = 0` |
| `20260531190905_AddDoctorVisitSearchIndex` | `IX_Visits_DoctorId_ScheduledDate` | NONCLUSTERED `(DoctorId, ScheduledDate)`, filter `[IsDeleted] = 0` |

Zastosowanie migracji:

```bash
dotnet ef database update --project src/ClinicManager.Web --startup-project src/ClinicManager.Web
```

## Analiza Query Plan

### Przygotowanie (SSMS / Azure Data Studio)

1. Uruchom kontener SQL Server (patrz README – sekcja Docker).
2. Zastosuj migracje na bazie `ClinicManagerDb`.
3. Dodaj kilku pacjentów i wizyt (przez aplikację lub seed).
4. W edytorze zapytań włącz **Include Actual Execution Plan** (`Ctrl+M`).
5. Opcjonalnie: `SET STATISTICS IO, TIME ON;` – porównanie logical reads i czasu CPU.

### Skrypty SQL

| Plik | Opis |
|------|------|
| `docs/sql/01-query-plan-przed-optymalizacja.sql` | Zapytania w stanie bez indeksów (lub po tymczasowym DROP) |
| `docs/sql/02-query-plan-po-optymalizacji.sql` | Te same zapytania po migracjach + weryfikacja indeksów |

### Oczekiwane różnice planów

| Zapytanie | PRZED | PO |
|-----------|-------|-----|
| PESEL = @pesel | **Clustered Index Scan** na `Patients` | **Index Seek** na `IX_Patients_Pesel` |
| Wizyty lekarza ORDER BY ScheduledDate | **Index Seek** na `IX_Visits_DoctorId` + **Sort** | **Index Seek** na `IX_Visits_DoctorId_ScheduledDate` (bez Sort) |

### Screenshoty do raportu PDF

Zrób **6 screenshotów** (3 zapytania × 2 stany):

1. PESEL – plan PRZED  
2. PESEL – plan PO  
3. Lista pacjentów (PESEL) – plan PRZED  
4. Lista pacjentów (PESEL) – plan PO  
5. Wizyty lekarza – plan PRZED  
6. Wizyty lekarza – plan PO  

**Jak zrobić screenshot w SSMS:** uruchom zapytanie z włączonym planem → zakładka **Execution Plan** (pod wynikami) → prawy przycisk na grafie → **Save Execution Plan** (`.sqlplan`) lub zrzut ekranu (`Win+Shift+S`).

**Składanie PDF:** Word / LibreOffice / Canva – jedna strona na zapytanie: nagłówek, SQL, screenshot planu, tabela STATISTICS IO (logical reads). Zapisz jako `docs/us9-raport-indeksy.pdf`.

### Przykładowa struktura raportu PDF

```
Strona 1 – Wprowadzenie (cel US#9, lista indeksów)
Strona 2 – Zapytanie PESEL – PRZED (screenshot + logical reads)
Strona 3 – Zapytanie PESEL – PO (screenshot + logical reads)
Strona 4 – Wizyty lekarza – PRZED
Strona 5 – Wizyty lekarza – PO
Strona 6 – Podsumowanie (skrót korzyści)
```

## Uwaga: wyszukiwanie częściowe PESEL / nazwisko

Wyszukiwanie `Contains` (LIKE `%tekst%`) **nie korzysta** z indeksu B-tree.  
`PatientService` dla **pełnego 11-cyfrowego PESEL** używa równości (`==`), co umożliwia **Index Seek**.

Wyszukiwanie po fragmencie nazwiska nadal wymaga skanowania – to akceptowalne przy małej liczbie rekordów w projekcie zaliczeniowym.

## Powiązane pliki

- `src/ClinicManager.Web/Data/ClinicDbContext.cs` – Fluent API
- `src/ClinicManager.Web/Services/PatientService.cs` – wyszukiwanie PESEL
- `src/ClinicManager.Web/Services/VisitService.cs` – filtrowanie po lekarzu
- `src/ClinicManager.Web/Migrations/` – migracje indeksów
