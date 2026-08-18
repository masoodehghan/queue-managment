# Queue Management API

[![Tests](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)

A queue-management REST API built with ASP.NET Core and .NET 8. It supports JWT-based authentication, queue ownership, joining queues, processing queue items in order, completing items, and estimating a user's waiting time.

## Features

- Register and log in with JWT authentication
- Create, list, update, and close queues
- Join an active queue
- List and update queue items
- Cancel/remove a queue item
- Process the next waiting item
- Complete the current in-progress item
- Automatically reorder waiting items after completion/removal
- View queue status, position, people ahead, and estimated waiting time
- View status across all queues the current user has joined
- Swagger/OpenAPI support in Development
- SQL Server persistence with Entity Framework Core
- FluentValidation request validation
- Serilog console and file logging

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- ASP.NET Core Identity
- JWT Bearer authentication
- FluentValidation
- Swagger / Swashbuckle
- Serilog
- xUnit for tests
- GitHub Actions for CI

## Project Structure

```text
queue-managment/
├── QueueManagement.Api/
│   ├── Controllers/
│   ├── Filters/
│   ├── Middleware/
│   ├── DependencyInjection.cs
│   └── Program.cs
├── QueueManagement.Application/
│   ├── Common/
│   ├── DTOs/
│   ├── Services/
│   └── Validators/
├── QueueManagement.Domain/
│   └── Entities/
├── QueueManagement.Infrastructure/
│   ├── Data/
│   ├── Migrations/
│   └── DependencyInjection.cs
├── QueueManagement.Tests/
├── .github/workflows/tests.yml
└── QueueManagement.sln
```

## Architecture

The solution is separated into four projects:

- **QueueManagement.Api** — HTTP endpoints, middleware, Swagger, and API configuration.
- **QueueManagement.Application** — queue use cases, DTOs, validation, interfaces, and application exceptions.
- **QueueManagement.Domain** — queue, queue-item, and user domain entities.
- **QueueManagement.Infrastructure** — EF Core `AppDbContext`, SQL Server, Identity, JWT configuration, migrations, and logging.

## Prerequisites

Install:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server, SQL Server Express, or LocalDB
- EF Core CLI tools if you want to apply migrations from the command line:

```bash
dotnet tool install --global dotnet-ef
```

## Getting Started

Clone the repository:

```bash
git clone https://github.com/masoodehghan/queue-managment.git
cd queue-managment
```

Restore packages:

```bash
dotnet restore
```

### Configure the database

The repository currently defaults to SQL Server LocalDB. You can override the connection string without modifying committed configuration:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=QueueManagementDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=QueueManagementDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

If you are on Windows and already have LocalDB installed, the connection string in `QueueManagement.Api/appsettings.json` can be used as-is.

### Configure JWT

For local development, override the JWT signing key instead of relying on a key committed in configuration:

```bash
export Jwt__Key="replace-this-with-a-long-random-development-secret"
```

PowerShell:

```powershell
$env:Jwt__Key="replace-this-with-a-long-random-development-secret"
```

For production, keep signing keys and database credentials in a secret manager or environment variables.

### Configure Swagger authentication

The repository currently contains a placeholder Swagger token URL. When running locally on port `5000`, set it to the API token endpoint:

```bash
export SwaggerSettings__TokenUrl="http://localhost:5000/api/auth/token"
```

PowerShell:

```powershell
$env:SwaggerSettings__TokenUrl="http://localhost:5000/api/auth/token"
```

### Apply migrations

```bash
dotnet ef database update \
  --project QueueManagement.Infrastructure \
  --startup-project QueueManagement.Api
```

### Run the API

```bash
dotnet run --project QueueManagement.Api --urls http://localhost:5000
```

In Development, Swagger is available at:

```text
http://localhost:5000/swagger
```

## Authentication

Most queue endpoints require a bearer token.

### Register

```http
POST /api/auth/register
Content-Type: application/json
```

```json
{
  "email": "user@example.com",
  "password": "YourStrongPassword123!",
  "fullName": "Example User"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "user@example.com",
  "password": "YourStrongPassword123!"
}
```

A successful login returns a JWT token. Send it to protected endpoints:

```http
Authorization: Bearer YOUR_TOKEN
```

The API also exposes `POST /api/auth/token` using `application/x-www-form-urlencoded`, which is used by the configured Swagger OAuth password flow.

## API Endpoints

### Authentication

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a user and receive a JWT | No |
| `POST` | `/api/auth/login` | Log in and receive a JWT | No |
| `POST` | `/api/auth/token` | Form-based token endpoint for Swagger/OAuth flow | No |

### Queues

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/queues` | Create a queue |
| `GET` | `/api/queues` | List queues owned by the current user |
| `GET` | `/api/queues/{id}` | Get a queue |
| `PUT` | `/api/queues/{id}` | Update a queue |
| `DELETE` | `/api/queues/{id}` | Close a queue |
| `GET` | `/api/queues/{id}/status` | Get queue status and the current user's position |

All queue endpoints require authentication. Updating, deleting, processing, and completing queue work is restricted by service rules to the queue owner where applicable.

### Queue Items

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/queues/{queueId}/items` | Join/add an item to a queue |
| `GET` | `/api/queues/{queueId}/items` | List active queue items |
| `PUT` | `/api/queues/{queueId}/items/{itemId}` | Update an item's name |
| `DELETE` | `/api/queues/{queueId}/items/{itemId}` | Cancel/remove an item |
| `POST` | `/api/queues/{queueId}/items/process-next` | Move the first waiting item to `InProgress` |
| `POST` | `/api/queues/{queueId}/items/complete-current` | Complete the current in-progress item |

### Current User Queue Status

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/my-queues/status` | Return status for queues the current user is waiting in or being processed in |

## Queue Flow

A typical queue lifecycle is:

```text
User joins queue
      │
      ▼
   Waiting
      │
      │ owner calls process-next
      ▼
 InProgress
      │
      │ owner calls complete-current
      ▼
  Completed
```

Removing an item marks it as cancelled. Completing or cancelling an item causes the remaining waiting items to be reordered.

## Example

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

Join the queue:

```bash
curl -X POST http://localhost:5000/api/queues/1/items \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "itemName": "Ticket #123"
  }'
```

Check your status:

```bash
curl http://localhost:5000/api/queues/1/status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Process the next item as the queue owner:

```bash
curl -X POST http://localhost:5000/api/queues/1/items/process-next \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Complete the current item:

```bash
curl -X POST http://localhost:5000/api/queues/1/items/complete-current \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Tests

The included `QueueManagement.Tests` project uses xUnit and EF Core's in-memory provider to test the queue service without requiring SQL Server.

The tests cover:

- Appending a new item at the correct position
- Estimated waiting-time calculation
- Processing the first waiting item
- Preventing non-owners from processing a queue
- Completing an in-progress item
- Reordering waiting items after completion
- Returning the current user's position and people-ahead count

Run the tests locally:

```bash
dotnet test QueueManagement.Tests/QueueManagement.Tests.csproj
```

## GitHub Actions Test Badge

The workflow at `.github/workflows/tests.yml` runs on pushes and pull requests targeting `main`.

The badge at the top of this README is:

```markdown
[![Tests](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/masoodehghan/queue-managment/actions/workflows/tests.yml)
```

After you add the workflow and test project to the repository and push to GitHub, the badge will automatically show the latest workflow status.

## Security Notes

- Do not use committed development JWT keys in production.
- Store production signing keys and database credentials outside source control.
- Use HTTPS in production.
- Review authorization rules before exposing queue-item update/remove operations to untrusted clients.

## License

No license file is currently included in this repository. Add a `LICENSE` file if you want to define how others may use, modify, or distribute the project.
