using Microsoft.EntityFrameworkCore;
using TrainerClientManagement.API.Models;

namespace TrainerClientManagement.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Trainer> Trainers { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>()
                .HasOne(client => client.Trainer)
                .WithMany(trainer => trainer.Clients)
                .HasForeignKey(client => client.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(appointment => appointment.Trainer)
                .WithMany()
                .HasForeignKey(appointment => appointment.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(appointment => appointment.Client)
                .WithMany()
                .HasForeignKey(appointment => appointment.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
