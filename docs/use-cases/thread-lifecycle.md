# Use Case: Thread Lifecycle

**Manager:** `ThreadWorkflowManager`  
**Engines used:** `ISpamDetectionEngine`, `IHotRankingEngine`

---

## Create Thread

**Actor:** Authenticated community member  
**Entry point:** `POST /communities/{id}/threads`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ThreadsController
    participant Mgr as ThreadWorkflowManager
    participant Spam as ISpamDetectionEngine
    participant Hot as IHotRankingEngine
    participant T as ForumThread
    participant TR as IThreadRepository
    participant UP as IUserProjectionRepository

    C->>Ctrl: POST /communities/{id}/threads {title, content?}
    Ctrl->>Mgr: CreateAsync(request)
    Mgr->>Spam: IsSpam(content ?? title, authorId)
    Spam-->>Mgr: false (or throw if spam)
    Mgr->>T: ForumThread.Create(communityId, authorId, title, content?)
    T-->>Mgr: thread (+ThreadCreated event)
    Mgr->>TR: AddAsync(thread)
    Mgr->>UP: GetByIdAsync(authorId)
    UP-->>Mgr: userProjection
    Mgr->>Hot: CalculateHotScore(createdAt, score=0, commentCount=0)
    Hot-->>Mgr: hotScore
    Mgr-->>Ctrl: ThreadResponse (with hotScore, author info)
    Ctrl-->>C: 201 Created
```

---

## Edit Thread

**Entry point:** `PUT /threads/{id}`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ThreadsController
    participant Mgr as ThreadWorkflowManager
    participant Spam as ISpamDetectionEngine
    participant T as ForumThread
    participant TR as IThreadRepository

    C->>Ctrl: PUT /threads/{id} {title, content?}
    Ctrl->>Mgr: EditAsync(request)
    Mgr->>TR: GetByIdAsync(threadId)
    TR-->>Mgr: thread (or null → 404)
    Mgr->>Spam: IsSpam(content ?? title, authorId)
    Spam-->>Mgr: false (or throw)
    Mgr->>T: thread.Edit(title, content?, now)
    T-->>Mgr: (+ThreadEdited event)
    Mgr->>TR: UpdateAsync(thread)
    Mgr-->>Ctrl: ThreadResponse
    Ctrl-->>C: 200 OK
```

---

## Delete / Lock / Pin Thread

All follow the same pattern: `GetByIdAsync` → domain method → `UpdateAsync`.

| Operation | Entry point | Domain method | Event |
|---|---|---|---|
| Delete | `DELETE /threads/{id}` | `thread.Delete(now)` | `ThreadDeleted` |
| Lock | `POST /threads/{id}/lock` | `thread.Lock(now)` | `ThreadLocked` |
| Pin | `POST /threads/{id}/pin` | `thread.Pin(now)` | `ThreadPinned` |

## Guard failures

| Guard | Error |
|---|---|
| Content flagged as spam | `InvalidOperationException` |
| Title empty | `ArgumentException` |
| Edit on deleted thread | `InvalidOperationException` |
| Edit on locked thread | `InvalidOperationException` |
| Delete already deleted | `InvalidOperationException` |
| Lock already locked | `InvalidOperationException` |
| Pin already pinned | `InvalidOperationException` |
