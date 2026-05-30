using System;

namespace ClinicManager.Web.DTOs;

public class MedicalRecordDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}