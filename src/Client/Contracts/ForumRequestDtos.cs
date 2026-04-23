using Forum.Application.Contracts;
using Forum.Domain.ValueObjects;

namespace Client.Contracts;

public sealed record CreateCommunityDto(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null);
public sealed record UpdateCommunityDto(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null);
public sealed record TransferOwnershipDto(Guid NewOwnerId);

public sealed record CreateThreadDto(string CommunitySlug, string Title, string? Content);
public sealed record EditThreadDto(string Title, string? Content);

public sealed record CreateCommentDto(Guid ThreadId, string Content, Guid? ParentCommentId);
public sealed record EditCommentDto(string Content);

public sealed record CastVoteDto(VoteTargetType TargetType, Guid TargetId, VoteDirection Direction);
public sealed record SwitchVoteDto(VoteDirection Direction);

public sealed record UpdateForumProfileDto(string? Bio, string? Signature);

public sealed class SearchQueryDto
{
    public string Query { get; init; } = string.Empty;
    public SearchScope Scope { get; init; } = SearchScope.All;
    public SearchSort Sort { get; init; } = SearchSort.Relevance;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
