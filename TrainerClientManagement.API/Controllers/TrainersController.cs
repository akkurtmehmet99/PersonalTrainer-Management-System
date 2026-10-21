using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainerClientManagement.API.Data;
using TrainerClientManagement.API.DTOs;
using TrainerClientManagement.API.Models;

namespace TrainerClientManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class TrainersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrainersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TrainerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TrainerDto>>> GetTrainers(CancellationToken cancellationToken)
        {
            var trainers = await _context.Trainers
                .AsNoTracking()
                .Select(trainer => new TrainerDto
                {
                    Id = trainer.Id,
                    Name = trainer.Name,
                    Specialization = trainer.Specialization,
                    Email = trainer.Email
                })
                .ToListAsync(cancellationToken);

            return Ok(trainers);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TrainerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrainerDto>> GetTrainer(int id, CancellationToken cancellationToken)
        {
            var trainer = await _context.Trainers
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => new TrainerDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Specialization = item.Specialization,
                    Email = item.Email
                })
                .FirstOrDefaultAsync(cancellationToken);

            return trainer is null ? NotFound() : Ok(trainer);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TrainerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TrainerDto>> CreateTrainer(
            CreateTrainerDto dto,
            CancellationToken cancellationToken)
        {
            var email = dto.Email.Trim();
            if (await _context.Trainers.AnyAsync(
                    trainer => trainer.Email.ToLower() == email.ToLower(),
                    cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "E-Mail-Adresse bereits vergeben",
                    detail: "Diese E-Mail-Adresse wird bereits von einem anderen Trainer verwendet.");
            }

            var trainer = new Trainer
            {
                Name = dto.Name.Trim(),
                Specialization = dto.Specialization.Trim(),
                Email = email
            };

            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetTrainer),
                new { id = trainer.Id },
                ToDto(trainer));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateTrainer(
            int id,
            UpdateTrainerDto dto,
            CancellationToken cancellationToken)
        {
            var trainer = await _context.Trainers.FindAsync([id], cancellationToken);
            if (trainer is null)
            {
                return NotFound();
            }

            var email = dto.Email.Trim();
            if (await _context.Trainers.AnyAsync(
                    item => item.Id != id && item.Email.ToLower() == email.ToLower(),
                    cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "E-Mail-Adresse bereits vergeben",
                    detail: "Diese E-Mail-Adresse wird bereits von einem anderen Trainer verwendet.");
            }

            trainer.Name = dto.Name.Trim();
            trainer.Specialization = dto.Specialization.Trim();
            trainer.Email = email;
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteTrainer(int id, CancellationToken cancellationToken)
        {
            var trainer = await _context.Trainers.FindAsync([id], cancellationToken);
            if (trainer is null)
            {
                return NotFound();
            }

            var hasAssignments = await _context.Clients.AnyAsync(
                    client => client.TrainerId == id,
                    cancellationToken)
                || await _context.Appointments.AnyAsync(
                    appointment => appointment.TrainerId == id,
                    cancellationToken);

            if (hasAssignments)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Trainer kann nicht gelöscht werden",
                    detail: "Zugeordnete Kunden oder Termine müssen zuerst einem anderen Trainer zugewiesen oder gelöscht werden.");
            }

            _context.Trainers.Remove(trainer);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private static TrainerDto ToDto(Trainer trainer) => new()
        {
            Id = trainer.Id,
            Name = trainer.Name,
            Specialization = trainer.Specialization,
            Email = trainer.Email
        };
    }
}
