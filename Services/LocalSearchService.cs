using System.Text;
using UniversalSearch.Models;

namespace UniversalSearch.Services;

/// <summary>Small local-first search for accessible paths, with bounded results and optional small-text scanning.</summary>
public sealed class LocalSearchService : ISearchService
{
    private const long MaxTextFileBytes = 512 * 1024;
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".md", ".csv", ".log" };

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string rootPath, string query, bool searchTextFiles,
        int maxResults = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath) || string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        maxResults = Math.Clamp(maxResults, 1, 500);
        var results = new List<SearchResult>(Math.Min(maxResults, 100));
        var pending = new Stack<string>();
        pending.Push(rootPath);

        await Task.Run(() =>
        {
            while (pending.Count > 0 && results.Count < maxResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();
                string[] directories;
                try { directories = Directory.EnumerateDirectories(current).ToArray(); }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { directories = Array.Empty<string>(); }

                foreach (var directory in directories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        // Do not follow junctions/symlinks: this avoids cycles and unexpected traversal outside the chosen tree.
                        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                            continue;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { continue; }

                    if (Path.GetFileName(directory).Contains(query, StringComparison.OrdinalIgnoreCase))
                        results.Add(new SearchResult(Path.GetFileName(directory), directory, true, "Folder name", SafeLastWriteUtc(directory)));
                    if (results.Count >= maxResults) break;
                    pending.Push(directory);
                }
                if (results.Count >= maxResults) break;

                string[] files;
                try { files = Directory.EnumerateFiles(current).ToArray(); }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { files = Array.Empty<string>(); }

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var name = Path.GetFileName(file);
                    if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        results.Add(new SearchResult(name, file, false, "File name", SafeLastWriteUtc(file)));
                    else if (searchTextFiles && TextExtensions.Contains(Path.GetExtension(file)) && SafeLength(file) <= MaxTextFileBytes)
                    {
                        try
                        {
                            using var reader = new StreamReader(file, detectEncodingFromByteOrderMarks: true);
                            if (reader.ReadToEnd().Contains(query, StringComparison.OrdinalIgnoreCase))
                                results.Add(new SearchResult(name, file, false, "Text content", SafeLastWriteUtc(file)));
                        }
                        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DecoderFallbackException) { }
                    }
                    if (results.Count >= maxResults) break;
                }
            }
        }, cancellationToken);
        return results;
    }

    private static DateTime SafeLastWriteUtc(string path)
    {
        try { return Directory.Exists(path) ? Directory.GetLastWriteTimeUtc(path) : File.GetLastWriteTimeUtc(path); }
        catch (IOException) { return DateTime.MinValue; }
        catch (UnauthorizedAccessException) { return DateTime.MinValue; }
    }

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch (IOException) { return long.MaxValue; }
        catch (UnauthorizedAccessException) { return long.MaxValue; }
    }
}