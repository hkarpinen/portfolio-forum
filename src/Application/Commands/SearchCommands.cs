namespace Forum.Application.Commands;

public enum SearchScope { All, Communities, Threads, Comments }
public enum SearchSort { Relevance, Newest, Hot, Top }

public sealed record SearchQueryCommand(string Query, SearchScope Scope, SearchSort Sort, int Page = 1, int PageSize = 20);
