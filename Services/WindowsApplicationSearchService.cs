using UniversalSearch.Models;

namespace UniversalSearch.Services;

/// <summary>
/// Searches the current user's and all users' Windows Start Menu shortcuts.
/// This deliberately avoids scanning every installed program directory.
/// </summary>
public sealed class WindowsApplicationSearchService
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        maxResults = Math.Clamp(maxResults, 1, 100);
        return await Task.Run<IReadOnlyList<SearchResult>>(() =>
        {
            var roots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
            }
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);

            var results = new List<SearchResult>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in roots)
            {
                var pending = new Stack<string>();
                pending.Push(root);

                while (pending.Count > 0 && results.Count < maxResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var current = pending.Pop();
                    if (!visited.Add(current))
                        continue;

                    IEnumerable<string> directories;
                    try { directories = Directory.EnumerateDirectories(current).ToArray(); }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { directories = Array.Empty<string>(); }

                    foreach (var directory in directories)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                                continue;
                        }
                        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { continue; }

                        pending.Push(directory);
                    }

                    IEnumerable<string> shortcuts;
                    try { shortcuts = Directory.EnumerateFiles(current, "*.lnk", SearchOption.TopDirectoryOnly).ToArray(); }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { shortcuts = Array.Empty<string>(); }

                    foreach (var shortcut in shortcuts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var name = Path.GetFileNameWithoutExtension(shortcut);
                        if (!name.Contains(query, StringComparison.OrdinalIgnoreCase))
                            continue;

                        results.Add(new SearchResult(name, shortcut, false, "Windows app shortcut", SafeLastWriteUtc(shortcut)));
                        if (results.Count >= maxResults)
                            break;
                    }
                }
            }

            return results;
        }, cancellationToken);
#else
        await Task.CompletedTask;
        return Array.Empty<SearchResult>();
#endif
    }

#if WINDOWS
    private static DateTime SafeLastWriteUtc(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { return DateTime.MinValue; }
    }
#endif
}