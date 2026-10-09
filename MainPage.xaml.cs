using UniversalSearch.Models;
using UniversalSearch.Services;

namespace UniversalSearch;

public partial class MainPage : ContentPage
{
    private readonly ISearchService _searchService;
    private readonly WindowsApplicationSearchService _applicationSearchService = new();
    private readonly WindowsSettingsSearchService _settingsSearchService = new();
    private CancellationTokenSource? _searchCancellation;

    public MainPage(ISearchService searchService)
    {
        InitializeComponent();
        _searchService = searchService;
#if ANDROID
        RootPathEntry.Placeholder = "Choose an accessible Android folder path";
        WindowsAppsOption.IsVisible = false;
        WindowsSettingsOption.IsVisible = false;
#endif
    }

    private async void OnBrowseFolderClicked(object? sender, EventArgs e)
    {
#if WINDOWS
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            var nativeWindow = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView
                as Microsoft.UI.Xaml.Window;
            if (nativeWindow is null)
            {
                await DisplayAlert("Folder picker unavailable", "The desktop window is not ready yet. Try again.", "OK");
                return;
            }

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
                RootPathEntry.Text = folder.Path;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Unable to choose folder", ex.Message, "OK");
        }
#else
        await DisplayAlert("Folder selection", "Android folder access requires integration with the system document picker. That is still pending; the current folder-path search is not yet Android-ready.", "OK");
#endif
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        var rootPath = RootPathEntry.Text?.Trim() ?? string.Empty;
        var query = QueryEntry.Text?.Trim() ?? string.Empty;
        var hasFileLocation = !string.IsNullOrWhiteSpace(rootPath) && Directory.Exists(rootPath);
        var searchApps = SearchWindowsAppsCheckBox.IsChecked;
        var searchSettings = SearchWindowsSettingsCheckBox.IsChecked;

        if (string.IsNullOrWhiteSpace(query))
        {
            await DisplayAlert("Enter a search term", "Type a filename, folder name, app name, setting, or supported text to search for.", "OK");
            return;
        }

        if (!hasFileLocation && !searchApps && !searchSettings)
        {
            await DisplayAlert("Choose a search source", "Choose an accessible folder, or select Windows app shortcuts or Settings pages.", "OK");
            return;
        }

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellationToken = _searchCancellation.Token;
        SearchButton.IsEnabled = false;
        StatusLabel.Text = "Searching locally…";
        ResultsLayout.Clear();

        try
        {
            var results = new List<SearchResult>();

            if (hasFileLocation)
            {
                var fileResults = await _searchService.SearchAsync(
                    rootPath, query, SearchTextCheckBox.IsChecked, 100, cancellationToken);
                results.AddRange(fileResults);
            }

#if WINDOWS
            if (searchApps)
            {
                StatusLabel.Text = "Searching Windows app shortcuts…";
                var appResults = await _applicationSearchService.SearchAsync(query, 50, cancellationToken);
                results.AddRange(appResults);
            }

            if (searchSettings)
            {
                StatusLabel.Text = "Searching common Windows Settings pages…";
                var settingResults = await _settingsSearchService.SearchAsync(query, 30, cancellationToken);
                results.AddRange(settingResults);
            }
#endif

            ShowResults(results);
            StatusLabel.Text = $"Found {results.Count} result(s). Local search only; results are not uploaded.";
        }
        catch (OperationCanceledException)
        {
            StatusLabel.Text = "Search cancelled.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Search could not be completed.";
            await DisplayAlert("Search error", ex.Message, "OK");
        }
        finally
        {
            SearchButton.IsEnabled = true;
        }
    }

    private void ShowResults(IReadOnlyList<SearchResult> results)
    {
        if (results.Count == 0)
        {
            ResultsLayout.Add(new Label { Text = "No matches found. Try another term or search source.", TextColor = Color.FromArgb("#9BA8B8"), Margin = new Thickness(0, 8) });
            return;
        }

        foreach (var result in results)
        {
            var icon = result.MatchType switch
            {
                "Windows app shortcut" => "🖥️",
                "Windows setting" => "⚙️",
                _ when result.IsDirectory => "📁",
                _ when IsImageFile(result.Name) => "🖼️",
                _ => "📄"
            };

            var button = new Button
            {
                Text = $"{icon}  {result.Name}\n{result.MatchType} · {result.FullPath}",
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Color.FromArgb("#141A24"),
                TextColor = Color.FromArgb("#F3F6FA"),
                BorderColor = Color.FromArgb("#283243"),
                BorderWidth = 1,
                CornerRadius = 10,
                Padding = new Thickness(12, 10),
                FontSize = 12,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            button.Clicked += async (_, _) => await OpenResultAsync(result);
            ResultsLayout.Add(button);
        }
    }

    private static bool IsImageFile(string name)
    {
        var extension = Path.GetExtension(name);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".heic", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private async Task OpenResultAsync(SearchResult result)
    {
        try
        {
#if WINDOWS
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = result.FullPath,
                UseShellExecute = true
            });
#else
            if (!result.IsDirectory)
                await Share.Default.RequestAsync(new ShareFileRequest { Title = result.Name, File = new ShareFile(result.FullPath) });
            else
                await DisplayAlert("Folder result", result.FullPath, "OK");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Unable to open item", ex.Message, "OK");
        }
    }
}
