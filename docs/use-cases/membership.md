# Use Case: Community Membership

**Manager:** `MembershipManager`

---

## Join Community

**Actor:** Authenticated user  
**Entry point:** `POST /communities/{id}/join`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as MembershipsController
    participant Mgr as MembershipManager
    participant M as CommunityMembership
    participant MR as IMembershipRepository

    C->>Ctrl: POST /communities/{id}/join
    Ctrl->>Mgr: JoinAsync(request)
    Mgr->>M: CommunityMembership.Create(communityId, userId, Member)
    M-->>Mgr: membership (+MembershipJoined event)
    Mgr->>MR: AddAsync(membership)
    Mgr-->>Ctrl: MembershipResponse
    Ctrl-->>C: 201 Created
```

---

## Leave Community

**Entry point:** `DELETE /memberships/{id}`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as MembershipsController
    participant Mgr as MembershipManager
    participant M as CommunityMembership
    participant MR as IMembershipRepository

    C->>Ctrl: DELETE /memberships/{id}
    Ctrl->>Mgr: LeaveAsync(request)
    Mgr->>MR: GetByIdAsync(membershipId)
    MR-->>Mgr: membership (or null → 404)
    Mgr->>M: membership.Leave(now)
    M-->>Mgr: (+MembershipLeft event)
    Mgr->>MR: UpdateAsync(membership)
    Mgr-->>Ctrl: MembershipResponse
    Ctrl-->>C: 200 OK
```

---

## Appoint / Remove Moderator

Both follow `GetByIdAsync` → domain method → `UpdateAsync`.

| Operation | Entry point | Domain method | Event |
|---|---|---|---|
| Appoint mod | `POST /memberships/{id}/moderator` | `membership.AppointModerator(now)` | `ModeratorAppointed` |
| Remove mod | `DELETE /memberships/{id}/moderator` | `membership.RemoveModerator(now)` | `ModeratorRemoved` |

## Guard failures

| Guard | Error |
|---|---|
| Leave already-left membership | `InvalidOperationException` |
| Appoint already-moderator | `InvalidOperationException` |
| Remove moderator from non-moderator | `InvalidOperationException` |
