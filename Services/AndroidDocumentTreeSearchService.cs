#if ANDROID
using Android.Content;
using Android.Provider;
using Android.Database;
using UniversalSearch.Models;

namespace UniversalSearch.Services;

public sealed class AndroidDocumentTreeSearchService : ISearchService
{
    private const long MaxTextFileBytes = 512 * 1024;
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".md", ".csv", ".log" };

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        string rootPath, string query, bool searchTextFiles, int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
        maxResults = Math.Clamp(maxResults, 1, 500);
        return Task.Run<IReadOnlyList<SearchResult>>(
            () => SearchDocumentTree(rootPath, query, searchTextFiles, maxResults, cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<SearchResult> SearchDocumentTree(
        string treeUriText, string query, bool searchTextFiles, int maxResults, CancellationToken token)
    {
        var resolver = Android.App.Application.Context.ContentResolver
            ?? throw new InvalidOperationException("Android document storage is unavailable.");
        var treeUri = Android.Net.Uri.Parse(treeUriText)
            ?? throw new InvalidOperationException("The selected folder URI is invalid.");
        var pending = new Stack<string>();
        var results = new List<SearchResult>(Math.Min(maxResults, 100));
        pending.Push(DocumentsContract.GetTreeDocumentId(treeUri));
        string[] projection =
        [
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType,
            DocumentsContract.Document.ColumnLastModified,
            DocumentsContract.Document.ColumnSize
        ];

        while (pending.Count > 0 && results.Count < maxResults)
        {
            token.ThrowIfCancellationRequested();
            var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, pending.Pop());
            ICursor? cursor = null;
            try
            {
                cursor = resolver.Query(childrenUri, projection, null, null, null);
                if (cursor is null) continue;
                var idColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
                var nameColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
                var mimeColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnMimeType);
                var modifiedColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnLastModified);
                var sizeColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnSize);

                while (cursor.MoveToNext() && results.Count < maxResults)
                {
                    token.ThrowIfCancellationRequested();
                    if (idColumn < 0 || nameColumn < 0 || mimeColumn < 0) break;
                    var documentId = cursor.GetString(idColumn);
                    var name = cursor.GetString(nameColumn) ?? string.Empty;
                    var mimeType = cursor.GetString(mimeColumn) ?? string.Empty;
                    var isDirectory = mimeType == DocumentsContract.Document.MimeTypeDir;
                    var modifiedMillis = modifiedColumn >= 0 && !cursor.IsNull(modifiedColumn)
                        ? cursor.GetLong(modifiedColumn) : 0L;
                    var modifiedUtc = modifiedMillis > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(modifiedMillis).UtcDateTime
                        : DateTime.MinValue;
                    var documentUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId);

                    if (isDirectory)
                    {
                        if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
                            results.Add(new SearchResult(name, documentUri.ToString()!, true, "Folder name", modifiedUtc));
                        pending.Push(documentId);
                        continue;
                    }
                    if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new SearchResult(name, documentUri.ToString()!, false, "File name", modifiedUtc));
                        continue;
                    }
                    if (!searchTextFiles || !TextExtensions.Contains(Path.GetExtension(name))) continue;
                    var size = sizeColumn >= 0 && !cursor.IsNull(sizeColumn) ? cursor.GetLong(sizeColumn) : -1L;
                    if (size < 0 || size > MaxTextFileBytes) continue;

                    try
                    {
                        using var stream = resolver.OpenInputStream(documentUri);
                        if (stream is null) continue;
                        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                        if (reader.ReadToEnd().Contains(query, StringComparison.OrdinalIgnoreCase))
                            results.Add(new SearchResult(name, documentUri.ToString()!, false, "Text content", modifiedUtc));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Java.IO.IOException) { }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Java.Lang.SecurityException) { }
            finally
            {
                cursor?.Close();
                cursor?.Dispose();
            }
        }
        return results;
    }
}
#endif
