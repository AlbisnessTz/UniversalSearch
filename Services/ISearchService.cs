using UniversalSearch.Models;

namespace UniversalSearch.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string rootPath,
        string query,
        bool searchTextFiles,
        int maxResults = 100,
        CancellationToken cancellationToken = default);
}