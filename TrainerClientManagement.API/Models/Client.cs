namespace TrainerClientManagement.API.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Zugeordneter Trainer
        public int TrainerId { get; set; }
        public Trainer? Trainer { get; set; }
    }
}
