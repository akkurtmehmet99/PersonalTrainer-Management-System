using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainerClientManagement.API.Data;
using TrainerClientManagement.API.DTOs;
using TrainerClientManagement.API.Models;

namespace TrainerClientManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // Alle Termine abrufen (Filterung mit ?trainerId=1)
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments(
            [FromQuery] int? trainerId,
            CancellationToken cancellationToken)
        {
            var query = _context.Appointments
                .AsNoTracking()
                .AsQueryable();

            if (trainerId.HasValue)
            {
                query = query.Where(a => a.TrainerId == trainerId.Value);
            }

            var appointments = await query
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    TrainerId = a.TrainerId,
                    TrainerName = a.Trainer != null ? a.Trainer.Name : string.Empty,
                    ClientId = a.ClientId,
                    ClientName = a.Client != null ? a.Client.Name : string.Empty
                })
                .ToListAsync(cancellationToken);

            return Ok(appointments);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentDto>> GetAppointment(
            int id,
            CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    TrainerId = a.TrainerId,
                    TrainerName = a.Trainer != null ? a.Trainer.Name : string.Empty,
                    ClientId = a.ClientId,
                    ClientName = a.Client != null ? a.Client.Name : string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            return appointment is null ? NotFound() : Ok(appointment);
        }

        // Einen Termin erstellen
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AppointmentDto>> PostAppointment(
            CreateAppointmentDto dto,
            CancellationToken cancellationToken)
        {
            if (dto.AppointmentDate == default)
            {
                ModelState.AddModelError(nameof(dto.AppointmentDate), "Bitte geben Sie ein gültiges Termindatum ein.");
                return ValidationProblem(ModelState);
            }

            var trainerExists = await _context.Trainers
                .AnyAsync(t => t.Id == dto.TrainerId, cancellationToken);
            if (!trainerExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültiger Trainer",
                    detail: "Ein Trainer mit der angegebenen ID wurde nicht gefunden.");
            }

            var clientExists = await _context.Clients
                .AnyAsync(c => c.Id == dto.ClientId, cancellationToken);
            if (!clientExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültiger Kunde",
                    detail: "Ein Kunde mit der angegebenen ID wurde nicht gefunden.");
            }

            var appointment = new Appointment
            {
                AppointmentDate = dto.AppointmentDate,
                Status = NormalizeStatus(dto.Status),
                TrainerId = dto.TrainerId,
                ClientId = dto.ClientId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(cancellationToken);

            var response = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == appointment.Id)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    TrainerId = a.TrainerId,
                    TrainerName = a.Trainer != null ? a.Trainer.Name : string.Empty,
                    ClientId = a.ClientId,
                    ClientName = a.Client != null ? a.Client.Name : string.Empty
                })
                .SingleAsync(cancellationToken);

            return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, response);
        }

        private static string NormalizeStatus(string status) => status.ToUpperInvariant() switch
        {
            "SCHEDULED" => "Scheduled",
            "COMPLETED" => "Completed",
            "CANCELLED" => "Cancelled",
            _ => status
        };
    }
}
