using System.ComponentModel.DataAnnotations;

namespace TrainerClientManagement.API.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = string.Empty;
    }

    public class CreateClientDto
    {
        [Required(ErrorMessage = "Der Name des Kunden ist erforderlich.")]
        [StringLength(100, ErrorMessage = "Der Kundenname darf maximal 100 Zeichen lang sein.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Die E-Mail-Adresse ist erforderlich.")]
        [StringLength(254, ErrorMessage = "Die E-Mail-Adresse darf maximal 254 Zeichen lang sein.")]
        [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Die Telefonnummer darf maximal 32 Zeichen lang sein.")]
        [Phone(ErrorMessage = "Bitte geben Sie eine gültige Telefonnummer ein.")]
        public string Phone { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Bitte wählen Sie einen gültigen Trainer aus.")]
        public int TrainerId { get; set; }
    }

    public sealed class UpdateClientDto : CreateClientDto
    {
    }
}
