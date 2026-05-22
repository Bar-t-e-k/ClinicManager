using ClinicManager.Web.DTOs;
using ClinicManager.Web.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Web.Mappers;

[Mapper]
public partial class PatientMapper
{
    public partial PatientDto PatientToPatientDto(Patient patient);
    public partial Patient CreatePatientDtoToPatient(CreateUpdatePatientDto dto);
    public partial void UpdatePatientFromDto(CreateUpdatePatientDto dto, Patient patient);

    public partial IQueryable<PatientDto> QueryablePatientToPatientDto(IQueryable<Patient> query);
}
