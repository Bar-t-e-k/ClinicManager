-- =============================================================================
-- US#9 – KROK „PO”: przywróć indeksy po teście „przed”
-- Uruchom w SSMS, potem skrypt 02-query-plan-po-optymalizacji.sql
-- =============================================================================

USE ClinicManagerDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Patients_Pesel' AND object_id = OBJECT_ID(N'Patients'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Patients_Pesel
        ON Patients (Pesel)
        WHERE ([IsDeleted] = 0);
    PRINT N'Utworzono IX_Patients_Pesel';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Visits_DoctorId_ScheduledDate' AND object_id = OBJECT_ID(N'Visits'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Visits_DoctorId_ScheduledDate
        ON Visits (DoctorId, ScheduledDate)
        WHERE ([IsDeleted] = 0);
    PRINT N'Utworzono IX_Visits_DoctorId_ScheduledDate';
END
GO

PRINT N'Indeksy przywrócone. Teraz uruchom: 02-query-plan-po-optymalizacji.sql';
GO
