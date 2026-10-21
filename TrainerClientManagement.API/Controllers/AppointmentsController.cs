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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments(
            [FromQuery] int? trainerId,
            [FromQuery] int? clientId,
            [FromQuery] string? status,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültiger Zeitraum",
                    detail: "Das Datum 'from' darf nicht nach dem Datum 'to' liegen.");
            }

            var normalizedStatus = status is null ? null : NormalizeStatus(status);
            if (status is not null && normalizedStatus is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültiger Terminstatus",
                    detail: "Der Status muss Scheduled, Completed oder Cancelled sein.");
            }

            var query = _context.Appointments.AsNoTracking();
            if (trainerId.HasValue)
            {
                query = query.Where(appointment => appointment.TrainerId == trainerId.Value);
            }

            if (clientId.HasValue)
            {
                query = query.Where(appointment => appointment.ClientId == clientId.Value);
            }

            if (normalizedStatus is not null)
            {
                query = query.Where(appointment => appointment.Status == normalizedStatus);
            }

            if (from.HasValue)
            {
                query = query.Where(appointment => appointment.AppointmentDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(appointment => appointment.AppointmentDate <= to.Value);
            }

            var appointments = await query
                .OrderBy(appointment => appointment.AppointmentDate)
                .Select(appointment => new AppointmentDto
                {
                    Id = appointment.Id,
                    AppointmentDate = appointment.AppointmentDate,
                    Status = appointment.Status,
                    DurationMinutes = appointment.DurationMinutes,
                    TrainerId = appointment.TrainerId,
                    TrainerName = appointment.Trainer != null ? appointment.Trainer.Name : string.Empty,
                    ClientId = appointment.ClientId,
                    ClientName = appointment.Client != null ? appointment.Client.Name : string.Empty
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
                .Where(item => item.Id == id)
                .Select(item => new AppointmentDto
                {
                    Id = item.Id,
                    AppointmentDate = item.AppointmentDate,
                    Status = item.Status,
                    DurationMinutes = item.DurationMinutes,
                    TrainerId = item.TrainerId,
                    TrainerName = item.Trainer != null ? item.Trainer.Name : string.Empty,
                    ClientId = item.ClientId,
                    ClientName = item.Client != null ? item.Client.Name : string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            return appointment is null ? NotFound() : Ok(appointment);
        }

        [HttpPost]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment(
            CreateAppointmentDto dto,
            CancellationToken cancellationToken)
        {
            if (dto.AppointmentDate == default)
            {
                ModelState.AddModelError(
                    nameof(dto.AppointmentDate),
                    "Bitte geben Sie ein gültiges Termindatum an.");
                return ValidationProblem(ModelState);
            }

            var trainerClientExists = await _context.Clients.AnyAsync(
                client => client.Id == dto.ClientId && client.TrainerId == dto.TrainerId,
                cancellationToken);
            if (!trainerClientExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültige Trainer-Kunden-Zuordnung",
                    detail: "Der Kunde ist dem angegebenen Trainer nicht zugeordnet.");
            }

            var normalizedStatus = NormalizeStatus(dto.Status);
            if (normalizedStatus is null)
            {
                ModelState.AddModelError(
                    nameof(dto.Status),
                    "Der Status muss Scheduled, Completed oder Cancelled sein.");
                return ValidationProblem(ModelState);
            }

            if (normalizedStatus == "Scheduled"
                && await HasScheduleConflictAsync(
                    dto.TrainerId,
                    dto.ClientId,
                    dto.AppointmentDate,
                    dto.DurationMinutes,
                    excludedAppointmentId: null,
                    cancellationToken))
            {
                return ScheduleConflict();
            }

            var appointment = new Appointment
            {
                AppointmentDate = dto.AppointmentDate,
                Status = normalizedStatus,
                DurationMinutes = dto.DurationMinutes,
                TrainerId = dto.TrainerId,
                ClientId = dto.ClientId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(cancellationToken);

            var response = await GetAppointmentDtoAsync(appointment.Id, cancellationToken);
            return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, response);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAppointment(
            int id,
            UpdateAppointmentDto dto,
            CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments.FindAsync([id], cancellationToken);
            if (appointment is null)
            {
                return NotFound();
            }

            if (dto.AppointmentDate == default)
            {
                ModelState.AddModelError(
                    nameof(dto.AppointmentDate),
                    "Bitte geben Sie ein gültiges Termindatum an.");
                return ValidationProblem(ModelState);
            }

            var trainerClientExists = await _context.Clients.AnyAsync(
                client => client.Id == dto.ClientId && client.TrainerId == dto.TrainerId,
                cancellationToken);
            if (!trainerClientExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültige Trainer-Kunden-Zuordnung",
                    detail: "Der Kunde ist dem angegebenen Trainer nicht zugeordnet.");
            }

            var normalizedStatus = NormalizeStatus(dto.Status);
            if (normalizedStatus is null)
            {
                ModelState.AddModelError(
                    nameof(dto.Status),
                    "Der Status muss Scheduled, Completed oder Cancelled sein.");
                return ValidationProblem(ModelState);
            }

            if (normalizedStatus == "Scheduled"
                && await HasScheduleConflictAsync(
                    dto.TrainerId,
                    dto.ClientId,
                    dto.AppointmentDate,
                    dto.DurationMinutes,
                    id,
                    cancellationToken))
            {
                return ScheduleConflict();
            }

            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.Status = normalizedStatus;
            appointment.DurationMinutes = dto.DurationMinutes;
            appointment.TrainerId = dto.TrainerId;
            appointment.ClientId = dto.ClientId;
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAppointment(int id, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments.FindAsync([id], cancellationToken);
            if (appointment is null)
            {
                return NotFound();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private async Task<bool> HasScheduleConflictAsync(
            int trainerId,
            int clientId,
            DateTime appointmentDate,
            int durationMinutes,
            int? excludedAppointmentId,
            CancellationToken cancellationToken)
        {
            var existingAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(appointment =>
                    appointment.Status == "Scheduled"
                    && (appointment.TrainerId == trainerId || appointment.ClientId == clientId)
                    && (!excludedAppointmentId.HasValue || appointment.Id != excludedAppointmentId.Value))
                .Select(appointment => new
                {
                    appointment.AppointmentDate,
                    appointment.DurationMinutes
                })
                .ToListAsync(cancellationToken);

            var requestedEnd = appointmentDate.AddMinutes(durationMinutes);
            return existingAppointments.Any(existing =>
            {
                var existingEnd = existing.AppointmentDate.AddMinutes(existing.DurationMinutes);
                return appointmentDate < existingEnd && existing.AppointmentDate < requestedEnd;
            });
        }

        private async Task<AppointmentDto> GetAppointmentDtoAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(appointment => appointment.Id == id)
                .Select(appointment => new AppointmentDto
                {
                    Id = appointment.Id,
                    AppointmentDate = appointment.AppointmentDate,
                    Status = appointment.Status,
                    DurationMinutes = appointment.DurationMinutes,
                    TrainerId = appointment.TrainerId,
                    TrainerName = appointment.Trainer != null ? appointment.Trainer.Name : string.Empty,
                    ClientId = appointment.ClientId,
                    ClientName = appointment.Client != null ? appointment.Client.Name : string.Empty
                })
                .SingleAsync(cancellationToken);
        }

        private ObjectResult ScheduleConflict() => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Terminkonflikt",
            detail: "Der Trainer oder Kunde hat im gewählten Zeitraum bereits einen anderen Termin.");

        private static string? NormalizeStatus(string status) => status.Trim().ToUpperInvariant() switch
        {
            "SCHEDULED" => "Scheduled",
            "COMPLETED" => "Completed",
            "CANCELLED" => "Cancelled",
            _ => null
        };
    }
}
