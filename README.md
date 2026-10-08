# Banking App

A small banking system: a REST API (ASP.NET Core, .NET 10, PostgreSQL) and a simple Blazor UI.

## Features

- Get the current balance of an account
- Deposit money into an account
- Withdraw money from an account (rejects overdrafts)
- List and create accounts
- Blazor web UI with account cards, quick amounts and validation

## Project layout

```
BankingApi/   REST API (EF Core + PostgreSQL)
BankingWeb/   Blazor Server UI that calls the API
docker-compose.yaml   Local stack: Postgres + API + UI
seed.sql      Optional sample accounts
.github/workflows/ci-cd.yaml   Build, push to ECR, deploy to ECS
```

## API endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/accounts` | List accounts |
| POST | `/api/accounts` | Create an account |
| GET | `/api/accounts/{id}/balance` | Current balance |
| POST | `/api/accounts/{id}/deposit` | Deposit `{ "amount": 100 }` |
| POST | `/api/accounts/{id}/withdraw` | Withdraw `{ "amount": 50 }` |
| GET | `/health` | Health check (used by the load balancer) |

Swagger UI is available at `/swagger`.

## Run locally

```
docker compose up -d --build
```

| Service | URL |
|---|---|
| UI | http://localhost:8081 |
| API + Swagger | http://localhost:8080/swagger |

Optional sample data (after the API has started once and created the tables):

```
Get-Content seed.sql | docker exec -i banking-postgres psql -U postgres -d banking
```

Stop everything with `docker compose down`.

## Configuration

| Setting (environment variable) | Used by | Purpose |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | API | PostgreSQL connection string |
| `BankingApi__BaseUrl` | UI | Address of the API, for example `http://api:8080/` |

## CI/CD

The GitHub Actions workflow in `.github/workflows/ci-cd.yaml` runs on every push and pull request to `main`:

1. **Build and test** the solution.
2. **Build and push** both Docker images to Amazon ECR, tagged with the commit SHA and `latest` (main only).
3. **Deploy** to ECS (prepared but commented out in the workflow until the cluster exists).

See [NOTES.md](NOTES.md) for prerequisites and the repository settings the pipeline needs.
