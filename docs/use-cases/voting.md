# Use Case: Voting

**Manager:** `VoteManager`

Votes target either a `Thread` or a `Comment` (`VoteTargetType`). Casting is an upsert — re-voting with a different direction switches; re-voting with the same direction is a no-op.

---

## Cast Vote (upsert)

**Actor:** Authenticated user  
**Entry point:** `POST /votes`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as VotesController
    participant Mgr as VoteManager
    participant V as Vote
    participant VR as IVoteRepository

    C->>Ctrl: POST /votes {targetType, targetId, direction}
    Ctrl->>Mgr: CastAsync(request)
    Mgr->>VR: GetByUserAndTargetAsync(userId, targetType, targetId)
    VR-->>Mgr: existing vote (or null)

    alt existing vote, different direction
        Mgr->>V: vote.SwitchDirection(newDirection, now)
        V-->>Mgr: (+VoteSwitched event)
        Mgr->>VR: UpdateAsync(vote)
    else existing vote, same direction
        Note over Mgr: no-op — return existing
    else no existing vote
        Mgr->>V: Vote.Create(targetType, targetId, userId, direction)
        V-->>Mgr: vote (+VoteCast event)
        Mgr->>VR: AddAsync(vote)
    end

    Mgr-->>Ctrl: VoteResponse
    Ctrl-->>C: 200 OK
```

---

## Retract Vote

**Entry point:** `DELETE /votes/{id}`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as VotesController
    participant Mgr as VoteManager
    participant V as Vote
    participant VR as IVoteRepository

    C->>Ctrl: DELETE /votes/{id}
    Ctrl->>Mgr: RetractAsync(request)
    Mgr->>VR: GetByIdAsync(voteId)
    VR-->>Mgr: vote (or null → 404)
    Mgr->>V: vote.Retract(now)
    V-->>Mgr: (+VoteRetracted event)
    Mgr->>VR: RemoveAsync(voteId)
    Mgr-->>Ctrl: VoteResponse
    Ctrl-->>C: 200 OK
```

## Guard failures

| Guard | Error |
|---|---|
| Switch to same direction | `InvalidOperationException` |
| Retract already retracted vote | `InvalidOperationException` |
