# Queue Management API

[![Tests](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)

A queue-management REST API built with **ASP.NET Core 10** and **.NET 10 LTS**.

The API provides JWT authentication, queue ownership, FIFO queue processing, queue membership, position tracking, estimated waiting times, SQL Server persistence, validation, structured logging, automated tests, and GitHub Actions CI.

## Features

- User registration and login with JWT authentication
- Create, update, list, and close queues
- Join active queues
- Prevent duplicate active queue membership
- FIFO queue processing
- Process the next waiting item
- Complete the current in-progress item
- Prevent multiple simultaneous in-progress items
- Update or cancel queue items with authorization checks
- Automatic queue-position reordering
- View queue position and people ahead
- Estimated waiting-time calculation
- View the current user's status across active queues
- SQL Server persistence with Entity Framework Core
- ASP.NET Core Identity
- Built-in ASP.NET Core OpenAPI support
- FluentValidation
- RFC 7807 `ProblemDetails` error responses
- Serilog request and application logging
- xUnit v3 tests
- GitHub Actions CI with code coverage

## Tech Stack

- .NET 10
- ASP.NET Core 10
- Entity Framework Core 10
- SQL Server
- ASP.NET Core Identity
- JWT Bearer authentication
- FluentValidation
- ASP.NET Core OpenAPI
- Serilog
- xUnit v3
- GitHub Actions

## Project Structure

```text
queue-managment/
├── QueueManagement.Api/
│   ├── Controllers/
│   ├── ErrorHandling/
│   ├── Extensions/
│   ├── Filters/
│   ├── DependencyInjection.cs
│   └── Program.cs
├── QueueManagement.Application/
│   ├── Common/
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   ├── DTOs/
│   ├── Services/
│   ├── Validators/
│   └── DependencyInjection.cs
├── QueueManagement.Domain/
│   └── Entities/
├── QueueManagement.Infrastructure/
│   ├── Authentication/
│   ├── Data/
│   ├── Migrations/
│   └── DependencyInjection.cs
├── QueueManagement.Tests/
├── .github/
│   └── workflows/
│       └── tests.yml
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
└── QueueManagement.sln
```

## Architecture

The solution is organized into four main projects.

### QueueManagement.Domain

Contains the core domain entities and enums.

Examples:

- `ApplicationUser`
- `Queue`
- `QueueItem`
- `QueueStatus`
- `QueueItemStatus`

### QueueManagement.Application

Contains application logic and abstractions.

Responsibilities include:

- Queue use cases
- DTOs
- Validation
- Application exceptions
- Service interfaces
- Database abstraction through `IApplicationDbContext`

### QueueManagement.Infrastructure

Contains infrastructure implementations.

Responsibilities include:

- Entity Framework Core
- SQL Server
- `AppDbContext`
- ASP.NET Core Identity persistence
- JWT token generation
- Database migrations

### QueueManagement.Api

Contains the ASP.NET Core HTTP layer.

Responsibilities include:

- Controllers
- Authentication and authorization
- OpenAPI
- Request validation
- Exception handling
- `ProblemDetails`
- Application startup
- Serilog configuration

## Requirements

Install:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server, SQL Server Express, or LocalDB

Verify the SDK:

```bash
dotnet --version
```

The repository includes a `global.json` file so the project uses the intended .NET 10 SDK.

## Getting Started

Clone the repository:

```bash
git clone https://github.com/masoodehghan/queue-managment.git
cd queue-managment
```

Restore dependencies:

```bash
dotnet restore QueueManagement.sln
dotnet restore QueueManagement.Tests/QueueManagement.Tests.csproj
```

## Configuration

### Database

The default configuration uses SQL Server LocalDB.

You can override the connection string with an environment variable.

Linux/macOS:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=QueueManagementDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=QueueManagementDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

### JWT

The JWT signing key is intentionally not stored in source control.

For local development, use .NET user secrets:

```bash
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-secret-at-least-32-bytes-long" --project QueueManagement.Api
```

Or use an environment variable:

```bash
export Jwt__Key="replace-with-a-random-secret-at-least-32-bytes-long"
```

PowerShell:

```powershell
$env:Jwt__Key="replace-with-a-random-secret-at-least-32-bytes-long"
```

Production secrets should be stored in your deployment platform's secret manager.

## Database Migrations

Install the EF Core CLI tool if needed:

```bash
dotnet tool install --global dotnet-ef
```

Apply migrations:

```bash
dotnet ef database update \
  --project QueueManagement.Infrastructure \
  --startup-project QueueManagement.Api
```

## Run the API

```bash
dotnet run --project QueueManagement.Api
```

In Development, the OpenAPI document is available at:

```text
/openapi/v1.json
```

## Authentication

Most queue endpoints require a bearer token.

### Register

```http
POST /api/auth/register
Content-Type: application/json
```

Example:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "fullName": "Example User"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json
```

Example:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

A successful response returns a JWT.

Use it on protected endpoints:

```http
Authorization: Bearer YOUR_TOKEN
```

The API also provides:

```http
POST /api/auth/token
```

This endpoint accepts `application/x-www-form-urlencoded` credentials.

## API Endpoints

### Authentication

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | No |
| `POST` | `/api/auth/login` | Log in and receive a JWT | No |
| `POST` | `/api/auth/token` | Form-based token endpoint | No |

### Queues

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/queues` | Create a queue |
| `GET` | `/api/queues` | Get queues owned by the current user |
| `GET` | `/api/queues/{id}` | Get a queue by ID |
| `PUT` | `/api/queues/{id}` | Update a queue |
| `DELETE` | `/api/queues/{id}` | Close a queue |
| `GET` | `/api/queues/{id}/status` | Get queue and current-user status |

All queue endpoints require authentication.

### Queue Items

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/queues/{queueId}/items` | Join a queue |
| `GET` | `/api/queues/{queueId}/items` | List active queue items |
| `PUT` | `/api/queues/{queueId}/items/{itemId}` | Update a queue item |
| `DELETE` | `/api/queues/{queueId}/items/{itemId}` | Cancel a queue item |
| `POST` | `/api/queues/{queueId}/items/process-next` | Process the next waiting item |
| `POST` | `/api/queues/{queueId}/items/complete-current` | Complete the current item |

### Current User Queue Status

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/my-queues/status` | Get the current user's status across active queues |

## Queue Rules

A queue item can have one of these states:

```text
Waiting
   │
   │ process-next
   ▼
InProgress
   │
   │ complete-current
   ▼
Completed
```

An item can also become:

```text
Cancelled
```

The API enforces the following rules:

- Only active queues accept new items.
- A user can have only one active item in the same queue.
- Queue processing follows FIFO order.
- Only the queue owner can process or complete queue items.
- Only the item owner or queue owner can update or cancel an active item.
- A queue can have only one `InProgress` item at a time.
- Completing or cancelling an item reorders the remaining waiting items.
- Closing a queue cancels its remaining active items.

## Example Requests

Create a queue:

```bash
curl -X POST http://localhost:5000/api/queues \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Customer Support",
    "description": "Support desk queue",
    "estimatedTimePerItem": 5
  }'
```

Join a queue:

```bash
curl -X POST http://localhost:5000/api/queues/1/items \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "itemName": "Ticket #123"
  }'
```

Check queue status:

```bash
curl http://localhost:5000/api/queues/1/status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Process the next item:

```bash
curl -X POST http://localhost:5000/api/queues/1/items/process-next \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Complete the current item:

```bash
curl -X POST http://localhost:5000/api/queues/1/items/complete-current \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Error Responses

The API uses RFC 7807-style `ProblemDetails`.

Example:

```json
{
  "type": "about:blank",
  "title": "Resource not found",
  "status": 404,
  "detail": "Queue 100 was not found.",
  "instance": "/api/queues/100",
  "traceId": "..."
}
```

Typical status codes include:

- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

## Tests

The test project uses:

- xUnit v3
- EF Core InMemory
- Microsoft.NET.Test.Sdk
- Coverlet

Run all tests:

```bash
dotnet test QueueManagement.Tests/QueueManagement.Tests.csproj
```

The test suite covers important queue behavior including:

- queue insertion
- estimated waiting times
- duplicate membership prevention
- FIFO processing
- queue-owner authorization
- queue-item authorization
- prevention of multiple in-progress items
- queue cancellation/reordering
- queue completion/reordering
- queue status and user position calculation

## Continuous Integration

GitHub Actions runs the test workflow for pushes and pull requests targeting `main`.

Workflow:

```text
.github/workflows/tests.yml
```

The workflow performs:

```text
Restore
   ↓
Build
   ↓
Test
   ↓
Code Coverage
```

The badge at the top of this README reflects the latest workflow result:

```markdown
[![Tests](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml)
```

## Package Management

NuGet package versions are centralized in:

```text
Directory.Packages.props
```

Shared project settings are defined in:

```text
Directory.Build.props
```

The .NET SDK is configured through:

```text
global.json
```

## Security

- Never commit JWT signing keys.
- Store production secrets outside source control.
- Use HTTPS in production.
- Keep database credentials in environment variables or a secret manager.
- Rotate secrets if they are ever exposed.
- Keep dependencies updated with supported .NET 10 package versions.

## License

No license file is currently included.

Add a `LICENSE` file if you want to define how others may use, modify, or distribute the project.
