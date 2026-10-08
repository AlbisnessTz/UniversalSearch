namespace UniversalSearch.Models;

public sealed record SearchResult(
    string Name,
    string FullPath,
    bool IsDirectory,
    string MatchType,
    DateTime LastModifiedUtc);