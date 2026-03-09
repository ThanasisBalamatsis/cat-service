# Cat Service API

A .NET 8 Web API that fetches cat images from [The Cat API](https://thecatapi.com/) as background jobs, stores metadata with breed-based tags in SQL Server, and serves them through a paginated REST API with filtering.

Built as a technical assignment demonstrating Clean Architecture, CQRS, background processing, and production-ready patterns.

---

## Deliverables Checklist

All assignment requirements have been implemented:

- ✅ `POST /api/cats/fetch` triggers background fetching of 25 cat images from The Cat API
- ✅ Background job processing using `BackgroundService` + `System.Threading.Channels` as an in-process job queue
- ✅ Job tracking — fetch endpoint returns a job identifier; track progress via `GET /api/jobs/{id}`
- ✅ No duplicate cat images — dual-layer prevention (pre-fetch DB check + unique constraint catch)
- ✅ Validation rules — FluentValidation with MediatR pipeline behavior
- ✅ Microsoft SQL Server with Entity Framework Core 8 (code-first migrations)
- ✅ Swagger documentation available at `/swagger`
- ✅ README with complete build/run instructions (this file)
- ✅ Unit tests — xUnit + FluentAssertions 7.0 + NSubstitute across all 4 layers
- ✅ Dockerfile + Docker Compose — multi-stage Docker build with SQL Server 2022

## Beyond the Requirements

Extra features added to demonstrate production-ready practices:

- **HTTP Resilience with Polly** — exponential backoff, 30-second timeout, automatic retry on `429 Too Many Requests` and `408 Request Timeout` responses from The Cat API
- **FluentValidation + MediatR `ValidationBehavior` Pipeline** — validation lives in the Application layer (not controllers), following Clean Architecture. Invalid requests are rejected before reaching any handler
- **Global Exception Handling** — ASP.NET Core 8 `IExceptionHandler` with `IProblemDetailsService` returning RFC 7807 ProblemDetails responses. Includes `WriteAsJsonAsync` fallback for non-JSON Accept headers
- **Docker Compose with Health Checks** — SQL Server 2022 container with `sqlcmd` health check; API container waits for healthy DB before starting. Database migrations run automatically on startup
- **API Versioning** — URL segment (`/api/v1/...`) + header-based (`X-Api-Version`) versioning via `Asp.Versioning`
- **Dual-Layer Duplicate Prevention** — pre-fetch existence check via `GetExistingCatIdsAsync` + unique constraint catch (SQL Server error codes 2601/2627) for concurrent job scenarios
- **Real-Time Job Progress Tracking** — `CatsFetched` count updates during processing, visible via `GET /api/jobs/{id}`
- **Source-Generated Logging** — `[LoggerMessage]` attributes for high-performance structured logging (zero-allocation log calls)
- **GitHub Workflow for Unit Tests** — Execution of unit tests step when a PR or a push to main takes place  
---

## Architecture

The project follows **Clean Architecture** with **CQRS** using **MediatR**:

```
src/
├── Domain/             # Entities (Cat, Tag, CatFetchJob) — no external dependencies
├── Application/        # Commands, Queries, Validators, Behaviors, Abstractions
│   ├── Behaviors/      #   └── ValidationBehavior (MediatR pipeline)
│   ├── Cats/           #   └── FetchCatsCommand, GetCatQuery, GetAllCatsQuery
│   ├── Jobs/           #   └── GetJobQuery
│   └── Abstractions/   #   └── ICatRepository, IJobRepository, ICatApiService, ICatFetchQueue
├── Infrastructure/     # EF Core, Repositories, Background Jobs, External API Client
│   ├── Persistence/    #   └── ApplicationDbContext, Repositories, Configurations, Migrations
│   ├── BackgroundJobs/ #   └── CatFetchBackgroundService, CatFetchQueue (Channels)
│   └── ExternalServices/ # └── CatApiService (HttpClient + Polly)
└── Api/                # Controllers, Exception Handlers, Swagger, Program.cs
    ├── Controllers/    #   └── CatsController, JobsController
    └── Exceptions/     #   └── ValidationExceptionHandler, GlobalExceptionHandler

test/
├── Domain.Tests.Unit/
├── Application.Tests.Unit/
├── Infrastructure.Tests.Unit/
└── Api.Tests.Unit/
```

**Key patterns:**

| Pattern | Implementation |
|---------|---------------|
| CQRS | Commands (`FetchCatsCommand`) and Queries (`GetCatQuery`, `GetAllCatsQuery`, `GetJobQuery`) separated via MediatR |
| Pipeline Behavior | `ValidationBehavior<TRequest, TResponse>` runs FluentValidation before every MediatR handler |
| Background Processing | `BackgroundService` consuming from `System.Threading.Channels` (unbounded, single-reader queue) |
| Repository Pattern | `ICatRepository` / `IJobRepository` abstractions in Application, implementations in Infrastructure |
| Exception Handling | `IExceptionHandler` chain: `ValidationExceptionHandler` → `GlobalExceptionHandler` with ProblemDetails |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (local instance) **or** [Docker](https://www.docker.com/)
- A free API key from [The Cat API](https://thecatapi.com/) (sign up → Dashboard → API Key)

---

## Getting Started

### Option 1: Local Development

#### 1. Clone the Repository

```bash
git clone https://github.com/your-username/cat-service.git
cd cat-service
```

#### 2. Get a Cat API Key

Sign up at [thecatapi.com](https://thecatapi.com/) and copy your API key from the dashboard.

#### 3. Configure User Secrets

The project uses .NET User Secrets to keep sensitive configuration out of source control. Run these commands from the repository root:

```bash
cd src/Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CatServiceDb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "CatsApiBaseUrl" "https://api.thecatapi.com/"
dotnet user-secrets set "xApiKey" "your-cat-api-key-here"
```

> **Note:** Adjust the connection string if your SQL Server uses SQL Authentication or a named instance (e.g., `Server=localhost\SQLEXPRESS;...;User Id=sa;Password=YourPassword`).

#### 4. Run the Application

```bash
dotnet run --project src/Api
```

The database is **created and migrated automatically** on startup — no need to run `dotnet ef database update` manually.

- **API:** `http://localhost:5125`
- **Swagger UI:** `http://localhost:5125/swagger`

---

### Option 2: Docker Compose

Docker Compose runs both SQL Server 2022 and the API in containers. No local SQL Server installation needed.

#### 1. Set Your Cat API Key

Create a `.env` file in the repository root:

```
CAT_API_KEY=your-cat-api-key-here
```

Or set the environment variable directly:

```bash
# Linux/macOS
export CAT_API_KEY=your-cat-api-key-here

# Windows PowerShell
$env:CAT_API_KEY = "your-cat-api-key-here"
```

#### 2. Start the Containers

```bash
docker-compose up --build
```

This starts:

- **SQL Server 2022** on port `1433` (SA password pre-configured in `docker-compose.yml` for development use)
- **Cat Service API** on port `5125`

The API waits for SQL Server to pass its health check before starting. Database migrations are applied automatically on startup.

#### 3. Access the API

- **API:** `http://localhost:5125`
- **Swagger UI:** `http://localhost:5125/swagger`

#### Stop and Clean Up

```bash
docker-compose down            # Stop containers (data persists in volume)
docker-compose down -v         # Stop containers and delete database volume
```

---

## API Endpoints

| Method | Endpoint | Description | Success | Error |
|--------|----------|-------------|---------|-------|
| `POST` | `/api/v1/cats/fetch` | Triggers a background job to fetch 25 cat images | `202 Accepted` | — |
| `GET` | `/api/v1/cats/{id}` | Gets a cat by database ID | `200 OK` | `404 Not Found` |
| `GET` | `/api/v1/cats?Page=1&PageSize=10&Tag=playful` | Paginated list of cats, filterable by tag | `200 OK` | `400 Bad Request` |
| `GET` | `/api/v1/jobs/{id}` | Gets the status of a fetch job | `200 OK` | `404 Not Found` |

### Validation Rules

| Parameter | Rule |
|-----------|------|
| `Page` | Must be greater than 0 |
| `PageSize` | Must be greater than 0 and less than or equal to 100 |

Invalid requests return a `400 Bad Request` with RFC 7807 ProblemDetails containing grouped validation errors.

### Example Workflow

```bash
# 1. Start a fetch job
curl -X POST http://localhost:5125/api/v1/cats/fetch

# Response: 202 Accepted
# {
#   "jobId": 1,
#   "status": "Pending"
# }
# Location: /api/v1/jobs/1

# 2. Check job status (poll until Completed)
curl http://localhost:5125/api/v1/jobs/1

# Response: { "id": 1, "status": "Completed", "catsFetched": 25, ... }

# 3. Browse fetched cats (paginated)
curl "http://localhost:5125/api/v1/cats?Page=1&PageSize=10"

# 4. Filter by breed temperament tag
curl "http://localhost:5125/api/v1/cats?Page=1&PageSize=10&Tag=playful"

# 5. Get a specific cat by ID
curl http://localhost:5125/api/v1/cats/1
```

---

## Running Tests

```bash
dotnet test
```

Tests are organized by architecture layer:

| Project | Coverage |
|---------|----------|
| `Domain.Tests.Unit` | Entity creation, validation, state transitions |
| `Application.Tests.Unit` | Command/Query handlers, validators, pipeline behavior |
| `Infrastructure.Tests.Unit` | Background service, queue, Cat API client |
| `Api.Tests.Unit` | Controllers, exception handlers |

---

## Design Decisions

### Image Storage — URLs Only (No Binary Storage)

The service stores **image URLs only** (served via The Cat API's CDN) rather than downloading and storing image binaries. This is a deliberate architectural decision:

**Why NOT store images in the database:**

- **Table/index bloat** — BLOBs in SQL Server dramatically increase row sizes (images range from 50KB to 5MB), causing page splits and degrading query performance across all operations on the table
- **Backup/restore impact** — Database backups become orders of magnitude larger and slower, increasing recovery time objectives
- **Connection pool exhaustion** — Long-running reads for large BLOBs hold connections open, reducing throughput under load
- **No CDN caching** — Database-stored images cannot leverage edge caching, forcing every request through the database
- **I/O pressure** — Mixed workloads (metadata queries + image reads) compete for the same I/O budget

**Production approach:** Download images during the background job and store them in a purpose-built object storage service (**AWS S3**, **Azure Blob Storage**, or **Supabase Storage**). These services provide CDN integration, automatic scaling, cost-effective storage tiers, and serve images without database involvement. The database would store the storage URL (not the original API URL) for reliability.

### Duplicate Prevention — Two-Layer Strategy

1. **Pre-fetch check** — Before inserting, queries the database for existing `CatId` values via `GetExistingCatIdsAsync` to skip known duplicates (fast path)
2. **Unique constraint catch** — Gracefully handles `DbUpdateException` from SQL Server unique index violations (error codes 2601/2627) for race conditions when multiple jobs run concurrently

This two-layer approach ensures correctness without pessimistic locking while keeping the common case fast.

### Background Job Architecture — Channels over Hangfire

Jobs are processed via `BackgroundService` + `System.Threading.Channels` rather than a heavier library like Hangfire or MassTransit. This provides:

- A lightweight, in-process queue with no external dependencies (no Redis, no additional database tables)
- Backpressure support via bounded channels
- Single-consumer pattern with `SingleReader = true` for optimal performance
- Appropriate complexity for an assignment scope while demonstrating the pattern

In a production system with multiple API instances, a distributed queue (RabbitMQ, Azure Service Bus) with a library like MassTransit would replace the in-process channel.
