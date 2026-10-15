using UniversalSearch.Services;
using Xunit;

namespace UniversalSearch.Tests;

public sealed class LocalSearchServiceTests
{
    [Fact]
    public async Task FindsFileByNameWithoutSearchingContents()
    {
        using var directory = new TemporaryDirectory();
        var filePath = Path.Combine(directory.Path, "photo-library.png");
        await File.WriteAllBytesAsync(filePath, [1, 2, 3]);

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "PHOTO", searchTextFiles: false);

        var result = Assert.Single(results);
        Assert.Equal("photo-library.png", result.Name);
        Assert.Equal(filePath, result.FullPath);
        Assert.Equal("File name", result.MatchType);
        Assert.False(result.IsDirectory);
    }

    [Fact]
    public async Task FindsMatchingFolderName()
    {
        using var directory = new TemporaryDirectory();
        var folderPath = Directory.CreateDirectory(Path.Combine(directory.Path, "Project-Notes")).FullName;

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "notes", searchTextFiles: false);

        var result = Assert.Single(results);
        Assert.Equal(folderPath, result.FullPath);
        Assert.True(result.IsDirectory);
        Assert.Equal("Folder name", result.MatchType);
    }

    [Fact]
    public async Task FindsTextInsideSupportedSmallTextFileWhenEnabled()
    {
        using var directory = new TemporaryDirectory();
        var filePath = Path.Combine(directory.Path, "journal.txt");
        await File.WriteAllTextAsync(filePath, "A private phrase: Blue lantern 47.");

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "BLUE LANTERN", searchTextFiles: true);

        var result = Assert.Single(results);
        Assert.Equal(filePath, result.FullPath);
        Assert.Equal("Text content", result.MatchType);
    }

    [Fact]
    public async Task FindsTextMatchAcrossReadBufferBoundary()
    {
        using var directory = new TemporaryDirectory();
        var filePath = Path.Combine(directory.Path, "boundary.txt");
        await File.WriteAllTextAsync(filePath, new string('a', 8 * 1024 - 1) + "Blue lantern");

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "Blue lantern", searchTextFiles: true);

        var result = Assert.Single(results);
        Assert.Equal(filePath, result.FullPath);
        Assert.Equal("Text content", result.MatchType);
    }

    [Fact]
    public async Task DoesNotSearchTextFilesLargerThanConfiguredLimit()
    {
        using var directory = new TemporaryDirectory();
        var filePath = Path.Combine(directory.Path, "large.txt");
        await File.WriteAllTextAsync(filePath, new string('a', 512 * 1024) + " hidden phrase");

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "hidden phrase", searchTextFiles: true);

        Assert.Empty(results);
    }

    [Fact]
    public async Task DoesNotSearchTextContentsWhenDisabled()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "journal.txt"), "Blue lantern 47.");

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "lantern", searchTextFiles: false);

        Assert.Empty(results);
    }

    [Fact]
    public async Task HonorsMaximumResultLimit()
    {
        using var directory = new TemporaryDirectory();
        for (var index = 0; index < 5; index++)
            await File.WriteAllTextAsync(Path.Combine(directory.Path, $"report-{index}.txt"), "sample");

        var service = new LocalSearchService();
        var results = await service.SearchAsync(directory.Path, "report", searchTextFiles: false, maxResults: 2);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ThrowsWhenSearchIsAlreadyCancelled()
    {
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var service = new LocalSearchService();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SearchAsync(directory.Path, "anything", searchTextFiles: false, cancellationToken: cancellation.Token));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"UniversalSearchTests-{Guid.NewGuid():N}");

        public TemporaryDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}