namespace TrainerClientManagement.API.Models
{
    public class Trainer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty; // Fachgebiet (Fitness, Yoga usw.)
        public string Email { get; set; } = string.Empty;
        
        // Ein Trainer kann mehrere Kunden betreuen.
        public ICollection<Client> Clients { get; set; } = new List<Client>();
    }
}
