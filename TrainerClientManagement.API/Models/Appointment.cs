namespace TrainerClientManagement.API.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed oder Cancelled
        public int DurationMinutes { get; set; } = 60;

        public int TrainerId { get; set; }
        public Trainer? Trainer { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }
    }
}
