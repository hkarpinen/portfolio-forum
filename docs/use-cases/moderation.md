# Use Case: Moderation

**Manager:** `ModerationManager`

---

## Ban User

**Actor:** Community moderator or owner  
**Entry point:** `POST /communities/{id}/bans`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ModerationController
    participant Mgr as ModerationManager
    participant Ban as CommunityBan
    participant Log as ModerationLog
    participant BR as IBanRepository
    participant LR as IModerationLogRepository

    C->>Ctrl: POST /communities/{id}/bans {userId, reason?}
    Ctrl->>Mgr: BanAsync(request)
    Mgr->>Ban: CommunityBan.Create(communityId, userId, reason?)
    Ban-->>Mgr: ban (+UserBanned event)
    Mgr->>BR: AddAsync(ban)
    Mgr->>Log: ModerationLog.Create(communityId, BanUser, performedBy, userId, reason?)
    Log-->>Mgr: log (+ModerationActionLogged event)
    Mgr->>LR: AddAsync(log)
    Mgr-->>Ctrl: BanResponse
    Ctrl-->>C: 201 Created
```

---

## Unban User

**Entry point:** `DELETE /bans/{id}`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ModerationController
    participant Mgr as ModerationManager
    participant Ban as CommunityBan
    participant Log as ModerationLog
    participant BR as IBanRepository
    participant LR as IModerationLogRepository

    C->>Ctrl: DELETE /bans/{id}
    Ctrl->>Mgr: UnbanAsync(request)
    Mgr->>BR: GetByIdAsync(banId)
    BR-->>Mgr: ban (or null → 404)
    Mgr->>Ban: ban.Unban(now)
    Ban-->>Mgr: (+UserUnbanned event)
    Mgr->>BR: RemoveAsync(ban.Id)
    Mgr->>Log: ModerationLog.Create(communityId, UnbanUser, performedBy, userId, null)
    Log-->>Mgr: log
    Mgr->>LR: AddAsync(log)
    Mgr-->>Ctrl: BanResponse
    Ctrl-->>C: 200 OK
```

---

## Log Moderation Action

**Entry point:** `POST /communities/{id}/moderation-log`  
Used to record free-form moderation actions (e.g. thread removal, content warning).

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ModerationController
    participant Mgr as ModerationManager
    participant Log as ModerationLog
    participant LR as IModerationLogRepository

    C->>Ctrl: POST /communities/{id}/moderation-log {action, targetUserId?, targetContent?}
    Ctrl->>Mgr: LogAsync(request)
    Mgr->>Log: ModerationLog.Create(communityId, action, performedBy, targetUserId?, content?)
    Log-->>Mgr: log (+ModerationActionLogged event)
    Mgr->>LR: AddAsync(log)
    Mgr-->>Ctrl: ModerationLogEntryResponse
    Ctrl-->>C: 201 Created
```

## Guard failures

| Guard | Error |
|---|---|
| Unban already unbanned | `InvalidOperationException` |
