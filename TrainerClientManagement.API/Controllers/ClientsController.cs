using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainerClientManagement.API.Data;
using TrainerClientManagement.API.DTOs;
using TrainerClientManagement.API.Models;

namespace TrainerClientManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        // Alle Kunden abrufen
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients(CancellationToken cancellationToken)
        {
            var clients = await _context.Clients
                .AsNoTracking()
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    TrainerId = c.TrainerId,
                    TrainerName = c.Trainer != null ? c.Trainer.Name : string.Empty
                })
                .ToListAsync(cancellationToken);

            return Ok(clients);
        }

        // Einen Kunden anhand seiner ID abrufen
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> GetClient(int id, CancellationToken cancellationToken)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    TrainerId = c.TrainerId,
                    TrainerName = c.Trainer != null ? c.Trainer.Name : string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (client == null)
            {
                return NotFound(new { message = "Kunde nicht gefunden." });
            }

            return Ok(client);
        }

        // Einen Kunden erstellen
        [HttpPost]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClientDto>> PostClient(
            CreateClientDto dto,
            CancellationToken cancellationToken)
        {
            var trainerName = await _context.Trainers
                .Where(t => t.Id == dto.TrainerId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (trainerName is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültiger Trainer",
                    detail: "Ein Trainer mit der angegebenen ID wurde nicht gefunden.");
            }

            var client = new Client
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                TrainerId = dto.TrainerId
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync(cancellationToken);

            var resultDto = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                Email = client.Email,
                Phone = client.Phone,
                TrainerId = client.TrainerId,
                TrainerName = trainerName
            };

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, resultDto);
        }
    }
}
