-- =============================================================================
-- US#9 – Analiza Query Plan PRZED optymalizacją (indeksy)
-- Baza: ClinicManagerDb (SQL Server)
-- Narzędzie: SSMS / Azure Data Studio
-- =============================================================================
-- INSTRUKCJA:
-- 1. Uruchom Docker z SQL Server (patrz README).
-- 2. Wykonaj migracje do migracji PRZED AddUniquePeselIndex / AddDoctorVisitSearchIndex
--    LUB tymczasowo usuń indeksy (sekcja DROP na końcu pliku).
-- 3. W SSMS: Query → Include Actual Execution Plan (Ctrl+M).
-- 4. Uruchom każde zapytanie osobno i zrób screenshot planu wykonania.
-- 5. Włącz statystyki I/O: SET STATISTICS IO, TIME ON;
-- =============================================================================

USE ClinicManagerDb;
GO

SET STATISTICS IO, TIME ON;
GO

-- ---------------------------------------------------------------------------
-- Zapytanie 1: Wyszukiwanie pacjenta po PESEL (PatientService.PeselExistsAsync)
-- Oczekiwany plan PRZED: Clustered Index Scan na Patients
-- ---------------------------------------------------------------------------
DECLARE @Pesel NVARCHAR(11) = N'85010112345';

SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM Patients AS p
    WHERE p.IsDeleted = 0
      AND p.Pesel = @Pesel
) THEN 1 ELSE 0 END AS PeselExists;
GO

-- ---------------------------------------------------------------------------
-- Zapytanie 2: Lista pacjentów – pełny PESEL (PatientService.GetAllPatientsAsync)
-- Oczekiwany plan PRZED: Clustered Index Scan + filtr
-- ---------------------------------------------------------------------------
DECLARE @SearchPesel NVARCHAR(11) = N'85010112345';

SELECT p.Id, p.FirstName, p.LastName, p.Pesel
FROM Patients AS p
WHERE p.IsDeleted = 0
  AND p.Pesel = @SearchPesel;
GO

-- ---------------------------------------------------------------------------
-- Zapytanie 3: Wizyty lekarza (VisitService.GetAllVisitsAsync)
-- Oczekiwany plan PRZED: Index Scan / Sort po DoctorId lub Clustered Index Scan
-- ---------------------------------------------------------------------------
DECLARE @DoctorId NVARCHAR(450) = (
    SELECT TOP 1 Id FROM AspNetUsers WHERE Email = N'lekarz@clinic.com'
);

SELECT v.Id, v.PatientId, v.DoctorId, v.ScheduledDate, v.Status
FROM Visits AS v
WHERE v.IsDeleted = 0
  AND v.DoctorId = @DoctorId
ORDER BY v.ScheduledDate;
GO

SET STATISTICS IO, TIME OFF;
GO

-- =============================================================================
-- OPCJONALNIE: symulacja stanu „przed indeksami” na aktualnej bazie
-- Uruchom TYLKO do porównania, potem przywróć indeksy (02-query-plan-po.sql)
-- =============================================================================
/*
DROP INDEX IF EXISTS IX_Patients_Pesel ON Patients;
DROP INDEX IF EXISTS IX_Visits_DoctorId_ScheduledDate ON Visits;
-- IX_Visits_DoctorId (FK) pozostaje – to domyślny indeks EF Core na DoctorId
*/
