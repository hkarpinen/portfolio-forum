# portfolio-forum

Community forum service. Users create and join communities, post threads, write nested comments, vote on content, and moderators manage membership, bans, and a mod action log. Built to demonstrate a realistic moderation-capable discussion platform without reaching for an off-the-shelf solution.

## What it does

- **Communities** — create a named community with a description and rules; members join publicly or by invite; moderators and owners manage the community
- **Threads** — post a thread (text or link) to a community; threads have a title, body, optional flair, and vote score
- **Comments** — nested threaded comments up to 5 levels deep (collapse beyond that); upvote/downvote; sort by hot, new, top
- **Voting** — user votes on threads and comments are deduplicated; score computed from net upvotes
- **Moderation** — mod queue for reported content, ban/unban members from a community, mod action log (who did what, when, why)
- **Domain events** — publishes `thread.created`, `comment.created` etc. over RabbitMQ for downstream consumers (notifications service)

## Stack

- .NET 8 / ASP.NET Core Web API
- PostgreSQL 17 (EF Core)
- RabbitMQ (event publishing via MassTransit)
- Clean Architecture: Domain → Application → Infrastructure → Client

## Running locally

```bash
# From repo root — requires postgres + rabbitmq (see infra/)
dotnet run --project src/Client
```

Or via the full stack:

```bash
docker compose -f infra/compose.dev.yaml up forum
```

## Structure

```
src/
  Domain/          Aggregates (Community, Thread, Comment, Vote, Ban),
                   value objects, domain events, domain engines (scoring)
  Application/     Managers (commands), query interfaces, repository interfaces
  Infrastructure/  EF Core, query implementations, repositories, MassTransit publishers
  Client/          ASP.NET Core controllers, FluentValidation validators, DI wiring
```

## API surface

| Controller | Routes | Purpose |
|---|---|---|
| `CommunitiesController` | `GET/POST /api/forum/communities`, `GET/PUT/DELETE …/{id}` | Community CRUD + membership |
| `ThreadsController` | `GET/POST /api/forum/communities/{id}/threads`, `PUT/DELETE …/{threadId}` | Thread CRUD + voting |
| `CommentsController` | `GET/POST /api/forum/threads/{id}/comments`, `PUT/DELETE …/{commentId}` | Nested comments + voting |
| `ModerationController` | `GET/POST /api/forum/communities/{id}/mod/*` | Mod queue, bans, mod log |

## Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Forum` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥ 32 chars) |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |

## Docs

- [Domain model & invariants](docs/Domain.md)
- Use cases: [`docs/use-cases/`](docs/use-cases/)

