using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Web.Mappers;

public interface IPatientMapper
{
    PatientDto PatientToPatientDto(Patient patient);
    Patient CreatePatientDtoToPatient(CreateUpdatePatientDto dto);
    void UpdatePatientFromDto(CreateUpdatePatientDto dto, Patient patient);
    IQueryable<PatientDto> QueryablePatientToPatientDto(IQueryable<Patient> query);
    PatientDetailsDto PatientToPatientDetailsDto(Patient patient);
    MedicalRecordDto MedicalRecordToMedicalRecordDto(MedicalRecord record);
}

[Mapper]
public partial class PatientMapper : IPatientMapper
{
    public partial PatientDto PatientToPatientDto(Patient patient);
    public partial Patient CreatePatientDtoToPatient(CreateUpdatePatientDto dto);
    public partial void UpdatePatientFromDto(CreateUpdatePatientDto dto, Patient patient);
    public partial IQueryable<PatientDto> QueryablePatientToPatientDto(IQueryable<Patient> query);
    public partial PatientDetailsDto PatientToPatientDetailsDto(Patient patient);
    public partial MedicalRecordDto MedicalRecordToMedicalRecordDto(MedicalRecord record);
}