# Use Case: Forum Profile

**Manager:** `ForumProfileManager`

Each user has at most one `ForumProfile`. The upsert pattern is used: create on first update, update thereafter.

---

## Create / Update Profile

**Actor:** Authenticated user  
**Entry point:** `PUT /profile`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ProfileController
    participant Mgr as ForumProfileManager
    participant FP as ForumProfile
    participant PR as IForumProfileRepository

    C->>Ctrl: PUT /profile {bio?, signature?}
    Ctrl->>Mgr: UpsertAsync(request)
    Mgr->>PR: GetByIdAsync(userId)
    PR-->>Mgr: profile (or null)

    alt profile exists
        Mgr->>FP: profile.Update(bio?, signature?, now)
        FP-->>Mgr: (+ForumProfileUpdated event)
        Mgr->>PR: UpdateAsync(profile)
    else no profile
        Mgr->>FP: ForumProfile.Create(userId, bio?, signature?)
        FP-->>Mgr: profile (+ForumProfileUpdated event)
        Mgr->>PR: AddAsync(profile)
    end

    Mgr-->>Ctrl: ForumProfileResponse
    Ctrl-->>C: 200 OK
```

## Guard failures

| Guard | Error |
|---|---|
| Bio > 500 characters | `ArgumentException` |
| Signature > 200 characters | `ArgumentException` |
