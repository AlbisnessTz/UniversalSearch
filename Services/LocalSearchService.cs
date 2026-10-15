using System.Text;
using UniversalSearch.Models;

namespace UniversalSearch.Services;

/// <summary>Small local-first search for accessible paths, with bounded results and optional small-text scanning.</summary>
public sealed class LocalSearchService : ISearchService
{
    private const long MaxTextFileBytes = 512 * 1024;
    private const int MaxTextCharactersToScan = 512 * 1024;
    private const int TextReadBufferSize = 8 * 1024;
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

                // Enumerate lazily so a directory containing many entries does not have to be
                // fully materialized in memory before we can honor cancellation/result limits.
                try
                {
                    foreach (var directory in Directory.EnumerateDirectories(current))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            // Do not follow junctions/symlinks: avoid cycles and unexpected traversal outside the chosen tree.
                            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                                continue;
                        }
                        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { continue; }

                        if (Path.GetFileName(directory).Contains(query, StringComparison.OrdinalIgnoreCase))
                            results.Add(new SearchResult(Path.GetFileName(directory), directory, true, "Folder name", SafeLastWriteUtc(directory)));

                        if (results.Count >= maxResults)
                            break;

                        pending.Push(directory);
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    // Keep any results already found and continue with other accessible folders.
                }

                if (results.Count >= maxResults)
                    break;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(current))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var name = Path.GetFileName(file);
                        if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new SearchResult(name, file, false, "File name", SafeLastWriteUtc(file)));
                        }
                        else if (searchTextFiles && TextExtensions.Contains(Path.GetExtension(file)) && SafeLength(file) <= MaxTextFileBytes)
                        {
                            try
                            {
                                if (ContainsQueryInBoundedTextFile(file, query, cancellationToken))
                                    results.Add(new SearchResult(name, file, false, "Text content", SafeLastWriteUtc(file)));
                            }
                            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DecoderFallbackException) { }
                        }

                        if (results.Count >= maxResults)
                            break;
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    // Access can change while searching; skip this folder's remaining files.
                }
            }
        }, cancellationToken);

        return results;
    }

    private static bool ContainsQueryInBoundedTextFile(string path, string query, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        var buffer = new char[TextReadBufferSize];
        var overlapLength = Math.Min(query.Length - 1, MaxTextCharactersToScan);
        var overlap = string.Empty;
        var charactersScanned = 0;

        // Bound both the read buffer and total characters examined. This also protects against
        // a file growing after SafeLength checked its size.
        while (charactersScanned < MaxTextCharactersToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = reader.Read(buffer, 0, Math.Min(buffer.Length, MaxTextCharactersToScan - charactersScanned));
            if (count == 0)
                break;

            var chunk = overlap + new string(buffer, 0, count);
            if (chunk.Contains(query, StringComparison.OrdinalIgnoreCase))
                return true;

            charactersScanned += count;
            overlap = overlapLength == 0
                ? string.Empty
                : chunk[^Math.Min(overlapLength, chunk.Length)..];
        }

        return false;
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