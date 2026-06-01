-- =============================================================================
-- US#9 – KROK „PRZED”: usuń indeksy, żeby zobaczyć wolniejszy plan
-- Uruchom w SSMS, potem skrypt 01-query-plan-przed-optymalizacja.sql
-- =============================================================================

USE ClinicManagerDb;
GO

DROP INDEX IF EXISTS IX_Patients_Pesel ON Patients;
DROP INDEX IF EXISTS IX_Visits_DoctorId_ScheduledDate ON Visits;
GO

PRINT N'Indeksy usunięte. Teraz uruchom: 01-query-plan-przed-optymalizacja.sql';
GO
