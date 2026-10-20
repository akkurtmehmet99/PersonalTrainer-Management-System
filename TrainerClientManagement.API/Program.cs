using Microsoft.EntityFrameworkCore;
using TrainerClientManagement.API.Data;
using TrainerClientManagement.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Datenbankdienst registrieren
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Trainer- und Kundenverwaltung mit Terminplanung",
        Version = "v1",
        Description = "RESTful Web-API zur Verwaltung von Trainern, Kunden und Terminen."
    });
});

var app = builder.Build();

// Globales Middleware zur Ausnahmebehandlung
app.UseMiddleware<GlobalExceptionMiddleware>();

// HTTP-Anforderungspipeline konfigurieren
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trainer Client API V1");
        c.RoutePrefix = string.Empty; // Swagger wird direkt auf der Startseite geöffnet.
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
