using System;

namespace ClinicManager.Web.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        public required string FileName { get; set; } 
        public required string FilePath { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    }
}