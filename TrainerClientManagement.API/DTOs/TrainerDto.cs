using System.ComponentModel.DataAnnotations;

namespace TrainerClientManagement.API.DTOs
{
    public class TrainerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateTrainerDto
    {
        [Required(ErrorMessage = "Der Name des Trainers ist erforderlich.")]
        [StringLength(100, ErrorMessage = "Der Trainername darf maximal 100 Zeichen lang sein.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Das Fachgebiet ist erforderlich.")]
        [StringLength(100, ErrorMessage = "Das Fachgebiet darf maximal 100 Zeichen lang sein.")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Die E-Mail-Adresse ist erforderlich.")]
        [StringLength(254, ErrorMessage = "Die E-Mail-Adresse darf maximal 254 Zeichen lang sein.")]
        [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class UpdateTrainerDto : CreateTrainerDto
    {
    }
}
