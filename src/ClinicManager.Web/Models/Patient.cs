using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Web.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Imię musi składać się z 2 do 100 znaków.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nazwisko musi składać się z 2 do 100 znaków.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "PESEL jest wymagany.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z dokładnie 11 cyfr.")]
        public string Pesel { get; set; } = string.Empty;

        [StringLength(50)]
        public string? InsuranceNumber { get; set; }

        /// <summary>Powiązanie rekordu pacjenta z kontem użytkownika (Identity). Pacjent loguje się i widzi tylko swoje wizyty.</summary>
        public string? UserId { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }
}