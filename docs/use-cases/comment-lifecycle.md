# Use Case: Comment Lifecycle

**Manager:** `CommentWorkflowManager`  
**Engines used:** `ISpamDetectionEngine`

---

## Create Comment

**Actor:** Authenticated user  
**Entry point:** `POST /threads/{id}/comments`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as CommentsController
    participant Mgr as CommentWorkflowManager
    participant Spam as ISpamDetectionEngine
    participant Co as Comment
    participant CR as ICommentRepository
    participant UP as IUserProjectionRepository

    C->>Ctrl: POST /threads/{id}/comments {content}
    Ctrl->>Mgr: CreateAsync(request)
    Mgr->>Spam: IsSpam(content, authorId)
    Spam-->>Mgr: false (or throw)
    Mgr->>Co: Comment.Create(threadId, authorId, content)
    Co-->>Mgr: comment (+CommentCreated event)
    Mgr->>CR: AddAsync(comment)
    Mgr->>UP: GetByIdAsync(authorId)
    UP-->>Mgr: userProjection
    Mgr-->>Ctrl: CommentResponse (with author info)
    Ctrl-->>C: 201 Created
```

---

## Edit Comment

**Entry point:** `PUT /comments/{id}`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as CommentsController
    participant Mgr as CommentWorkflowManager
    participant Spam as ISpamDetectionEngine
    participant Co as Comment
    participant CR as ICommentRepository

    C->>Ctrl: PUT /comments/{id} {content}
    Ctrl->>Mgr: EditAsync(request)
    Mgr->>CR: GetByIdAsync(commentId)
    CR-->>Mgr: comment (or null → 404)
    Mgr->>Spam: IsSpam(content, authorId)
    Spam-->>Mgr: false (or throw)
    Mgr->>Co: comment.Edit(content, now)
    Co-->>Mgr: (+CommentEdited event)
    Mgr->>CR: UpdateAsync(comment)
    Mgr-->>Ctrl: CommentResponse
    Ctrl-->>C: 200 OK
```

---

## Delete Comment

**Entry point:** `DELETE /comments/{id}`

Same pattern: `GetByIdAsync` → `comment.Delete(now)` → `UpdateAsync`.

## Guard failures

| Guard | Error |
|---|---|
| Content flagged as spam | `InvalidOperationException` |
| Content empty | `ArgumentException` |
| Edit on deleted comment | `InvalidOperationException` |
| Delete already deleted | `InvalidOperationException` |
