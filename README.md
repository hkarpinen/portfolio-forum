# portfolio-forum

Reddit-style community forum service. Users create communities, post threads, comment, vote, and moderators manage membership and bans.

## Stack

- .NET 8 / ASP.NET Core Web API
- PostgreSQL 17 (EF Core)
- RabbitMQ (event publishing)
- Clean Architecture: Domain → Application → Infrastructure → Client

## Running locally

```bash
dotnet run --project src/Client
```

Or via the full stack in `portfolio-infra`:

```bash
docker compose up forum
```

## Structure

```
src/
  Domain/          Aggregates, value objects, domain events, domain engines
  Application/     Managers (commands), query interfaces
  Infrastructure/  EF Core, query implementations, repositories, messaging
  Client/          ASP.NET Core controllers, validators, DI wiring
```

## Docs

- [Domain model & invariants](docs/Domain.md)
- Use cases: [`docs/use-cases/`](docs/use-cases/)

## Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Forum` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥ 32 chars) |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |
