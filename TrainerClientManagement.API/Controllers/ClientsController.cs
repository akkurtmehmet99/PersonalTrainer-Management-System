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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients(
            [FromQuery] int? trainerId,
            CancellationToken cancellationToken)
        {
            var query = _context.Clients.AsNoTracking();
            if (trainerId.HasValue)
            {
                query = query.Where(client => client.TrainerId == trainerId.Value);
            }

            var clients = await query
                .OrderBy(client => client.Name)
                .Select(client => new ClientDto
                {
                    Id = client.Id,
                    Name = client.Name,
                    Email = client.Email,
                    Phone = client.Phone,
                    TrainerId = client.TrainerId,
                    TrainerName = client.Trainer != null ? client.Trainer.Name : string.Empty
                })
                .ToListAsync(cancellationToken);

            return Ok(clients);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> GetClient(int id, CancellationToken cancellationToken)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => new ClientDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Email = item.Email,
                    Phone = item.Phone,
                    TrainerId = item.TrainerId,
                    TrainerName = item.Trainer != null ? item.Trainer.Name : string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            return client is null ? NotFound() : Ok(client);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ClientDto>> CreateClient(
            CreateClientDto dto,
            CancellationToken cancellationToken)
        {
            var trainerName = await _context.Trainers
                .Where(trainer => trainer.Id == dto.TrainerId)
                .Select(trainer => trainer.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (trainerName is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Geçersiz eğitmen",
                    detail: "Belirtilen eğitmen bulunamadı.");
            }

            var email = dto.Email.Trim();
            if (await _context.Clients.AnyAsync(
                    client => client.Email.ToLower() == email.ToLower(),
                    cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "E-posta adresi kullanımda",
                    detail: "Bu e-posta adresi başka bir danışan tarafından kullanılıyor.");
            }

            var client = new Client
            {
                Name = dto.Name.Trim(),
                Email = email,
                Phone = dto.Phone.Trim(),
                TrainerId = dto.TrainerId
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetClient),
                new { id = client.Id },
                ToDto(client, trainerName));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateClient(
            int id,
            UpdateClientDto dto,
            CancellationToken cancellationToken)
        {
            var client = await _context.Clients.FindAsync([id], cancellationToken);
            if (client is null)
            {
                return NotFound();
            }

            var trainerExists = await _context.Trainers.AnyAsync(
                trainer => trainer.Id == dto.TrainerId,
                cancellationToken);
            if (!trainerExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Geçersiz eğitmen",
                    detail: "Belirtilen eğitmen bulunamadı.");
            }

            if (client.TrainerId != dto.TrainerId
                && await _context.Appointments.AnyAsync(
                    appointment => appointment.ClientId == id,
                    cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Danışan başka eğitmene aktarılamıyor",
                    detail: "Danışanın randevuları varken eğitmeni değiştirilemez. Önce randevuları güncelleyin veya silin.");
            }

            var email = dto.Email.Trim();
            if (await _context.Clients.AnyAsync(
                    item => item.Id != id && item.Email.ToLower() == email.ToLower(),
                    cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "E-posta adresi kullanımda",
                    detail: "Bu e-posta adresi başka bir danışan tarafından kullanılıyor.");
            }

            client.Name = dto.Name.Trim();
            client.Email = email;
            client.Phone = dto.Phone.Trim();
            client.TrainerId = dto.TrainerId;
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteClient(int id, CancellationToken cancellationToken)
        {
            var client = await _context.Clients.FindAsync([id], cancellationToken);
            if (client is null)
            {
                return NotFound();
            }

            if (await _context.Appointments.AnyAsync(
                    appointment => appointment.ClientId == id,
                    cancellationToken))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Danışan silinemiyor",
                    detail: "Danışana bağlı randevular önce silinmelidir.");
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private static ClientDto ToDto(Client client, string trainerName) => new()
        {
            Id = client.Id,
            Name = client.Name,
            Email = client.Email,
            Phone = client.Phone,
            TrainerId = client.TrainerId,
            TrainerName = trainerName
        };
    }
}
