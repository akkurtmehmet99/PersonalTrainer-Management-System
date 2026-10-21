# Personal Trainer Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8-512BD4)](https://learn.microsoft.com/ef/core/)
[![SQLite](https://img.shields.io/badge/Datenbank-SQLite-003B57?logo=sqlite)](https://www.sqlite.org/)

Eine ASP.NET-Core-Web-API zur Verwaltung von Personal Trainern, Kunden und individuellen Trainingsterminen.

**GitHub:** [akkurtmehmet99/PersonalTrainer-Management-System](https://github.com/akkurtmehmet99/PersonalTrainer-Management-System)

## Funktionen

- Vollständige CRUD-REST-API für Trainer, Kunden und Termine
- Persistente Datenspeicherung mit Entity Framework Core 8 und SQLite
- DTOs für Anfragen und Antworten sowie Validierung mit Data Annotations
- Terminfilter nach Status, Zeitraum, Trainer und Kunde
- Verhindert zeitliche Überschneidungen für Trainer und Kunden
- Prüft Trainer-Kunden-Zuordnungen und eindeutige E-Mail-Adressen
- Zentrale Fehlerbehandlung mit `application/problem+json` und Trace-ID
- Interaktive API-Dokumentation mit Swagger UI
- Automatische Erstellung der SQLite-Datenbank beim ersten Start

## Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Installation und Start

```bash
git clone https://github.com/akkurtmehmet99/PersonalTrainer-Management-System.git
cd PersonalTrainer-Management-System
dotnet restore TrainerClientManagement.API.sln
dotnet run --project TrainerClientManagement.API/TrainerClientManagement.API.csproj
```

Beim Start wird die SQLite-Datei `trainer-client-management.db` im aktuellen Arbeitsverzeichnis erstellt. Die Verbindungszeichenfolge kann über `ConnectionStrings:DefaultConnection` in `TrainerClientManagement.API/appsettings.json` angepasst werden. Die Swagger-Oberfläche ist unter der von der Anwendung ausgegebenen Adresse verfügbar.

> Das Datenbankschema wird beim ersten Start erstellt. `EnsureCreated` aktualisiert kein vorhandenes Schema. Verwenden Sie für Schemaänderungen an bestehenden Datenbanken EF-Core-Migrationen.
>
> Vorhandene SQL-Server-Daten werden nicht automatisch nach SQLite migriert. Sichern und übertragen Sie benötigte Daten vor dem Wechsel separat.

## API-Endpunkte

| Ressource | Methode | Endpunkt | Beschreibung |
|---|---|---|---|
| Trainer | `GET` | `/api/trainers` | Alle Trainer abrufen |
| Trainer | `GET` | `/api/trainers/{id}` | Trainerdetails abrufen |
| Trainer | `POST` | `/api/trainers` | Trainer anlegen |
| Trainer | `PUT` | `/api/trainers/{id}` | Trainer aktualisieren |
| Trainer | `DELETE` | `/api/trainers/{id}` | Trainer ohne zugeordnete Datensätze löschen |
| Kunde | `GET` | `/api/clients?trainerId=1` | Kunden optional nach Trainer filtern |
| Kunde | `GET` | `/api/clients/{id}` | Kundendetails abrufen |
| Kunde | `POST` | `/api/clients` | Kunden anlegen |
| Kunde | `PUT` | `/api/clients/{id}` | Kunden aktualisieren |
| Kunde | `DELETE` | `/api/clients/{id}` | Kunden ohne Termine löschen |
| Termin | `GET` | `/api/appointments` | Termine filtern |
| Termin | `GET` | `/api/appointments/{id}` | Termindetails abrufen |
| Termin | `POST` | `/api/appointments` | Termin anlegen |
| Termin | `PUT` | `/api/appointments/{id}` | Termin aktualisieren |
| Termin | `DELETE` | `/api/appointments/{id}` | Termin löschen |

Die Terminliste unterstützt die Abfrageparameter `trainerId`, `clientId`, `status`, `from` und `to`. Zulässige Statuswerte sind `Scheduled`, `Completed` und `Cancelled`. Die Termindauer muss zwischen 15 und 240 Minuten liegen. Zeitliche Überschneidungen mit anderen geplanten Terminen desselben Trainers oder Kunden werden abgelehnt.

## Beispielanfragen

### Trainer anlegen

```http
POST /api/trainers
Content-Type: application/json

{
  "name": "Anna Müller",
  "specialization": "Krafttraining",
  "email": "anna@example.com"
}
```

### Kunde anlegen

```http
POST /api/clients
Content-Type: application/json

{
  "name": "Max Mustermann",
  "email": "max@example.com",
  "phone": "+49 30 12345678",
  "trainerId": 1
}
```

### Trainingstermin anlegen

```http
POST /api/appointments
Content-Type: application/json

{
  "appointmentDate": "2026-11-01T10:00:00Z",
  "durationMinutes": 60,
  "status": "Scheduled",
  "trainerId": 1,
  "clientId": 1
}
```

## Antworten und Fehler

- Erfolgreich erstellt: `201 Created` mit `Location`-Header
- Erfolgreich aktualisiert oder gelöscht: `204 No Content`
- Validierungsfehler: `400 Bad Request` mit feldbezogenen `ValidationProblemDetails`
- Ressource nicht gefunden: `404 Not Found`
- E-Mail-Duplikat, Terminüberschneidung oder Verletzung einer Zuordnungsregel: `409 Conflict`
- Unerwarteter Fehler: generische `500`-ProblemDetails-Antwort mit `traceId`; interne Details werden nicht offengelegt

## Technologie

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core 8
- SQLite
- DTO-basierte Anfrage- und Antwortmodelle
- Swagger / OpenAPI

## Lizenz

Dieses Projekt wird im [GitHub-Repository](https://github.com/akkurtmehmet99/PersonalTrainer-Management-System) entwickelt und bereitgestellt.
