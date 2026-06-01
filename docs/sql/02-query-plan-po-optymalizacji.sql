-- =============================================================================
-- US#9 – Analiza Query Plan PO optymalizacji (Non-Clustered Indexes)
-- Baza: ClinicManagerDb (SQL Server)
-- =============================================================================
-- Wymagane migracje EF Core:
--   • 20260530095912_AddUniquePeselIndex
--   • 20260531190905_AddDoctorVisitSearchIndex
--
-- dotnet ef database update --project src/ClinicManager.Web
-- =============================================================================

USE ClinicManagerDb;
GO

-- Weryfikacja indeksów
SELECT
    i.name AS IndexName,
    t.name AS TableName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.filter_definition AS FilterDefinition
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON i.object_id = t.object_id
WHERE t.name IN (N'Patients', N'Visits')
  AND i.name IS NOT NULL
  AND i.name NOT LIKE N'PK_%'
ORDER BY t.name, i.name;
GO

SET STATISTICS IO, TIME ON;
GO

-- ---------------------------------------------------------------------------
-- Zapytanie 1: PESEL – unikalność / istnienie
-- Oczekiwany plan PO: Index Seek na IX_Patients_Pesel (filtered unique)
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
-- Zapytanie 2: Lista pacjentów – pełny PESEL
-- Oczekiwany plan PO: Index Seek na IX_Patients_Pesel
-- ---------------------------------------------------------------------------
DECLARE @SearchPesel NVARCHAR(11) = N'85010112345';

SELECT p.Id, p.FirstName, p.LastName, p.Pesel
FROM Patients AS p
WHERE p.IsDeleted = 0
  AND p.Pesel = @SearchPesel;
GO

-- ---------------------------------------------------------------------------
-- Zapytanie 3: Wizyty lekarza posortowane po dacie
-- Oczekiwany plan PO: Index Seek na IX_Visits_DoctorId_ScheduledDate
--                      (bez osobnego Sort)
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

-- Przywrócenie indeksów (jeśli usunięto w skrypcie „przed”):
/*
CREATE UNIQUE NONCLUSTERED INDEX IX_Patients_Pesel
    ON Patients (Pesel)
    WHERE ([IsDeleted] = 0);

CREATE NONCLUSTERED INDEX IX_Visits_DoctorId_ScheduledDate
    ON Visits (DoctorId, ScheduledDate)
    WHERE ([IsDeleted] = 0);
*/
