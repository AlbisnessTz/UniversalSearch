using UniversalSearch.Models;

namespace UniversalSearch.Services;

/// <summary>
/// Searches a curated set of common Windows Settings pages by their display names and aliases.
/// This is intentionally not presented as an exhaustive index of every setting.
/// </summary>
public sealed class WindowsSettingsSearchService
{
    private sealed record SettingEntry(string Name, string Uri, string[] Keywords);

    private static readonly SettingEntry[] Settings =
    {
        new("Display", "ms-settings:display", new[] { "screen", "brightness", "resolution", "monitor" }),
        new("Sound", "ms-settings:sound", new[] { "audio", "volume", "speaker", "microphone", "headphones" }),
        new("Bluetooth & devices", "ms-settings:bluetooth", new[] { "bluetooth", "pair", "mouse", "keyboard", "printer", "device" }),
        new("Network & internet", "ms-settings:network", new[] { "wifi", "wi-fi", "internet", "ethernet", "network", "vpn", "hotspot" }),
        new("Personalization", "ms-settings:personalization", new[] { "theme", "wallpaper", "background", "colors", "lock screen" }),
        new("Apps", "ms-settings:appsfeatures", new[] { "installed apps", "uninstall", "default apps", "startup apps" }),
        new("Accounts", "ms-settings:yourinfo", new[] { "profile", "sign in", "email", "user account" }),
        new("Date & time", "ms-settings:dateandtime", new[] { "clock", "timezone", "time zone", "calendar" }),
        new("Language & region", "ms-settings:regionlanguage", new[] { "language", "keyboard language", "region", "typing" }),
        new("Accessibility", "ms-settings:easeofaccess", new[] { "magnifier", "narrator", "captions", "text size", "contrast" }),
        new("Privacy & security", "ms-settings:privacy", new[] { "privacy", "permissions", "camera permission", "microphone permission" }),
        new("Windows Update", "ms-settings:windowsupdate", new[] { "update", "updates", "upgrade", "restart update" }),
        new("Storage", "ms-settings:storagesense", new[] { "disk space", "temporary files", "storage sense", "drive space" }),
        new("Power & battery", "ms-settings:powersleep", new[] { "battery", "power", "sleep", "screen timeout" }),
        new("Printers & scanners", "ms-settings:printers", new[] { "print", "scanner", "printer" }),
        new("Camera", "ms-settings:camera", new[] { "webcam", "camera privacy" }),
        new("Notifications", "ms-settings:notifications", new[] { "alerts", "do not disturb", "focus" }),
        new("Recovery", "ms-settings:recovery", new[] { "reset pc", "recovery options", "advanced startup" }),
        new("About this PC", "ms-settings:about", new[] { "device name", "windows version", "system information", "computer specs" })
    };

    public Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults = 30,
        CancellationToken cancellationToken = default)
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());

        cancellationToken.ThrowIfCancellationRequested();
        var results = Settings
            .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || s.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || query.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Take(Math.Clamp(maxResults, 1, 50))
            .Select(s => new SearchResult(s.Name, s.Uri, false, "Windows setting", DateTime.MinValue))
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
#else
        return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
#endif
    }
}
