using ClinicManager.Web.DTOs;

namespace ClinicManager.Web.Services;

public interface IReportService
{
    Task<byte[]> GeneratePatientMonthlyCostReportAsync(int patientId, int year, int month);
    Task<byte[]> GenerateDoctorMonthlyCostReportAsync(string doctorId, int year, int month);
}