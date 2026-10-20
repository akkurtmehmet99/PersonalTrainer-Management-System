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

        // Alle Trainer abrufen
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TrainerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TrainerDto>>> GetTrainers(CancellationToken cancellationToken)
        {
            var trainers = await _context.Trainers
                .AsNoTracking()
                .Select(t => new TrainerDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Specialization = t.Specialization,
                    Email = t.Email
                })
                .ToListAsync(cancellationToken);

            return Ok(trainers);
        }

        // Einen Trainer anhand seiner ID abrufen
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TrainerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrainerDto>> GetTrainer(int id, CancellationToken cancellationToken)
        {
            var trainer = await _context.Trainers
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TrainerDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Specialization = t.Specialization,
                    Email = t.Email
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (trainer == null)
            {
                return NotFound(new { message = "Trainer nicht gefunden." });
            }

            return Ok(trainer);
        }

        // Einen Trainer erstellen
        [HttpPost]
        [ProducesResponseType(typeof(TrainerDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<TrainerDto>> PostTrainer(
            CreateTrainerDto dto,
            CancellationToken cancellationToken)
        {
            var trainer = new Trainer
            {
                Name = dto.Name,
                Specialization = dto.Specialization,
                Email = dto.Email
            };

            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync(cancellationToken);

            var resultDto = new TrainerDto
            {
                Id = trainer.Id,
                Name = trainer.Name,
                Specialization = trainer.Specialization,
                Email = trainer.Email
            };

            return CreatedAtAction(nameof(GetTrainer), new { id = trainer.Id }, resultDto);
        }
    }
}
