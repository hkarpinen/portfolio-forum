# Use Case: Community Lifecycle

**Manager:** `CommunityWorkflowManager`

---

## Create Community

**Actor:** Authenticated user  
**Entry point:** `POST /communities`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as CommunitiesController
    participant Mgr as CommunityWorkflowManager
    participant Co as Community
    participant CR as ICommunityRepository

    C->>Ctrl: POST /communities {name, visibility, description?, imageUrl?}
    Ctrl->>Mgr: CreateAsync(request)
    Mgr->>Co: Community.Create(name, visibility, ownerId, description?, imageUrl?)
    Co-->>Mgr: community (+CommunityCreated event)
    Mgr->>CR: AddAsync(community)
    Mgr-->>Ctrl: CommunityResponse
    Ctrl-->>C: 201 Created
```

---

## Update Community

**Entry point:** `PUT /communities/{id}`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as CommunitiesController
    participant Mgr as CommunityWorkflowManager
    participant Co as Community
    participant CR as ICommunityRepository

    C->>Ctrl: PUT /communities/{id} {name, visibility, description?, imageUrl?}
    Ctrl->>Mgr: UpdateAsync(request)
    Mgr->>CR: GetByIdAsync(communityId)
    CR-->>Mgr: community (or null → 404)
    Mgr->>Co: community.Update(name, visibility, now, description?, imageUrl?)
    Co-->>Mgr: (+CommunityUpdated event)
    Mgr->>CR: UpdateAsync(community)
    Mgr-->>Ctrl: CommunityResponse
    Ctrl-->>C: 200 OK
```

---

## Transfer Ownership

**Entry point:** `POST /communities/{id}/transfer-ownership`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as CommunitiesController
    participant Mgr as CommunityWorkflowManager
    participant Co as Community
    participant CR as ICommunityRepository

    C->>Ctrl: POST /communities/{id}/transfer-ownership {newOwnerId}
    Ctrl->>Mgr: TransferOwnershipAsync(request)
    Mgr->>CR: GetByIdAsync(communityId)
    CR-->>Mgr: community (or null → 404)
    Mgr->>Co: community.TransferOwnership(newOwnerId, now)
    Co-->>Mgr: (+CommunityOwnershipTransferred event)
    Mgr->>CR: UpdateAsync(community)
    Mgr-->>Ctrl: CommunityResponse
    Ctrl-->>C: 200 OK
```

## Guard failures

| Guard | Error |
|---|---|
| Name empty | `ArgumentException` |
