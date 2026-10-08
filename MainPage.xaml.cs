using UniversalSearch.Models;
using UniversalSearch.Services;

namespace UniversalSearch;

public partial class MainPage : ContentPage
{
    private readonly ISearchService _searchService;
    private CancellationTokenSource? _searchCancellation;

    public MainPage(ISearchService searchService)
    {
        InitializeComponent();
        _searchService = searchService;
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        var rootPath = RootPathEntry.Text?.Trim() ?? string.Empty;
        var query = QueryEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            await DisplayAlert("Choose a location", "Enter a folder path that this app can access.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            await DisplayAlert("Enter a search term", "Type a filename, folder name, or word to search for.", "OK");
            return;
        }

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        SearchButton.IsEnabled = false;
        StatusLabel.Text = "Searching locally…";
        ResultsLayout.Clear();
        try
        {
            var results = await _searchService.SearchAsync(rootPath, query, SearchTextCheckBox.IsChecked, 100, _searchCancellation.Token);
            ShowResults(results);
            StatusLabel.Text = $"Found {results.Count} result(s). Results remain local to this device.";
        }
        catch (OperationCanceledException) { StatusLabel.Text = "Search cancelled."; }
        catch (Exception ex)
        {
            StatusLabel.Text = "Search could not be completed.";
            await DisplayAlert("Search error", ex.Message, "OK");
        }
        finally { SearchButton.IsEnabled = true; }
    }

    private void ShowResults(IReadOnlyList<SearchResult> results)
    {
        if (results.Count == 0)
        {
            ResultsLayout.Add(new Label { Text = "No matches found. Try another term or location.", TextColor = Color.FromArgb("#9BA8B8"), Margin = new Thickness(0, 8) });
            return;
        }
        foreach (var result in results)
        {
            var button = new Button
            {
                Text = $"{(result.IsDirectory ? "📁" : "📄")}  {result.Name}\n{result.MatchType} · {result.FullPath}",
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Color.FromArgb("#141A24"), TextColor = Color.FromArgb("#F3F6FA"),
                BorderColor = Color.FromArgb("#283243"), BorderWidth = 1, CornerRadius = 10,
                Padding = new Thickness(12, 10), FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation
            };
            button.Clicked += async (_, _) => await OpenResultAsync(result);
            ResultsLayout.Add(button);
        }
    }

    private async Task OpenResultAsync(SearchResult result)
    {
        try
        {
#if WINDOWS
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = result.FullPath, UseShellExecute = true });
#else
            if (!result.IsDirectory)
                await Share.Default.RequestAsync(new ShareFileRequest { Title = result.Name, File = new ShareFile(result.FullPath) });
            else
                await DisplayAlert("Folder result", result.FullPath, "OK");
#endif
        }
        catch (Exception ex) { await DisplayAlert("Unable to open item", ex.Message, "OK"); }
    }
}