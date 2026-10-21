using System.ComponentModel.DataAnnotations;

namespace TrainerClientManagement.API.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
    }

    public class CreateAppointmentDto
    {
        public DateTime AppointmentDate { get; set; }

        [Range(15, 240, ErrorMessage = "Die Dauer muss zwischen 15 und 240 Minuten betragen.")]
        public int DurationMinutes { get; set; } = 60;

        [Required(ErrorMessage = "Der Terminstatus ist erforderlich.")]
        [RegularExpression("^(?i:Scheduled|Completed|Cancelled)$", ErrorMessage = "Der Terminstatus muss Scheduled, Completed oder Cancelled lauten.")]
        public string Status { get; set; } = "Scheduled";

        [Range(1, int.MaxValue, ErrorMessage = "Bitte wählen Sie einen gültigen Trainer aus.")]
        public int TrainerId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Bitte wählen Sie einen gültigen Kunden aus.")]
        public int ClientId { get; set; }
    }

    public sealed class UpdateAppointmentDto : CreateAppointmentDto
    {
    }
}
