using ClinicManager.Web.DTOs;
using ClinicManager.Web.Mappers;
using ClinicManager.Web.Models;

namespace ClinicManager.Tests;

public class PatientMapperTests
{
    private readonly PatientMapper _mapper;

    public PatientMapperTests()
    {
        _mapper = new PatientMapper();
    }

    [Fact]
    public void PatientToPatientDto_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var patient = new Patient { Id = 1, FirstName = "Jan", LastName = "Kowalski", Pesel = "12345678901" };

        // Act
        var result = _mapper.PatientToPatientDto(patient);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patient.Id, result.Id);
        Assert.Equal(patient.FirstName, result.FirstName);
        Assert.Equal(patient.LastName, result.LastName);
        Assert.Equal(patient.Pesel, result.Pesel);
    }

    [Fact]
    public void CreatePatientDtoToPatient_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var dto = new CreateUpdatePatientDto { FirstName = "Anna", LastName = "Nowak", Pesel = "98765432109" };

        // Act
        var result = _mapper.CreatePatientDtoToPatient(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.FirstName, result.FirstName);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.Equal(dto.Pesel, result.Pesel);
    }

    [Fact]
    public void UpdatePatientFromDto_ShouldModifyExistingPatientProperties()
    {
        // Arrange
        var dto = new CreateUpdatePatientDto { FirstName = "Zofia", LastName = "Wiśniewska", Pesel = "55555555555" };
        var patient = new Patient { Id = 99, FirstName = "Stary", LastName = "Pacjent", Pesel = "11111111111" };

        // Act
        _mapper.UpdatePatientFromDto(dto, patient);

        // Assert
        Assert.Equal(99, patient.Id);
        Assert.Equal("Zofia", patient.FirstName);
        Assert.Equal("Wiśniewska", patient.LastName);
        Assert.Equal("55555555555", patient.Pesel);
    }

    [Fact]
    public void QueryablePatientToPatientDto_ShouldMapProjectionCorrectly()
    {
        // Arrange
        var patientsList = new List<Patient>
        {
            new Patient { Id = 1, FirstName = "Jan" },
            new Patient { Id = 2, FirstName = "Anna" }
        }.AsQueryable();

        // Act
        var result = _mapper.QueryablePatientToPatientDto(patientsList).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Jan", result[0].FirstName);
        Assert.Equal(2, result[1].Id);
        Assert.Equal("Anna", result[1].FirstName);
    }

    [Fact]
    public void PatientToPatientDetailsDto_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var patient = new Patient { Id = 10, FirstName = "Piotr", LastName = "Zieliński" };

        // Act
        var result = _mapper.PatientToPatientDetailsDto(patient);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patient.Id, result.Id);
        Assert.Equal(patient.FirstName, result.FirstName);
        Assert.Equal(patient.LastName, result.LastName);
    }

    [Fact]
    public void MedicalRecordToMedicalRecordDto_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var record = new MedicalRecord
        {
            Id = 500,
            FileName = "test_raport.pdf",
            FilePath = "/uploads/test_raport.pdf"
        };

        // Act
        var result = _mapper.MedicalRecordToMedicalRecordDto(record);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(record.Id, result.Id);
    }
}