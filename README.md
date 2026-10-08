# UniversalSearch

**A lightweight, privacy-first search app for Windows and Android.**

UniversalSearch is being developed as a local-first utility to help people find files, folders, images by filename, Windows Start Menu app shortcuts, selected Windows Settings pages, and text inside a limited set of small text files. Local search is designed to work without an account or internet connection; online search will remain optional and separate.

## Current development status

This is an early MVP foundation, not a release-ready app. The shared .NET MAUI interface and local filesystem search are present. The Windows UI now includes options for searching Start Menu shortcuts and a curated list of common Windows Settings pages. The latest changes still require a successful CI build and runtime testing before they can be considered verified.

### Current scope and limitations

- File and folder names are searched inside a folder selected by the user.
- Image files (including JPG/JPEG, PNG, GIF, BMP, WebP, TIFF, HEIC and SVG) are currently found by **filename**, not by image contents or visual similarity.
- Optional full-text search is limited to small TXT, MD, CSV and LOG files (up to 512 KiB each).
- Windows app discovery searches Start Menu shortcut files; it is not a complete inventory of every installed app.
- Windows Settings search covers a curated list of common pages and aliases. It is not an exhaustive search of every Windows setting.
- Android's system document-picker and persistent folder-access integration is not complete yet. Do not consider Android file search ready until that integration is implemented and tested.
- PDF and DOCX text extraction, image-content search, indexing/caching and optional web search are planned work, not implemented features.

## Product principles

- **Local-first:** never upload local files for indexing.
- **Lightweight:** bounded results, asynchronous scans, and cancellation.
- **Privacy:** user-selected locations; no account required.
- **Cross-platform:** shared C# logic with platform-specific file access.
- **Honest scope:** Android storage is permission-limited; support will follow Android's scoped-storage rules.

## Next milestones

1. Verify the Windows build in CI and fix any errors reported by the actual build.
2. Test folder, image filename, app shortcut and Settings results on Windows.
3. Implement Android document-provider access using Android's supported storage APIs.
4. Add robust indexing and performance safeguards for large folders.
5. Add supported PDF/DOCX text extraction and tests.
6. Consider image-content search and optional web search as separate, opt-in features.

## Build prerequisites

- A supported .NET SDK matching the target framework in UniversalSearch.csproj.
- .NET MAUI workload and platform tooling for the platform you want to run.
- For Windows, a compatible Windows SDK and Visual Studio Build Tools/Visual Studio workload.

Check the environment with dotnet --info and dotnet workload list.

## Build

```bash
dotnet restore
dotnet build
```

Use Visual Studio or the .NET MAUI tooling to select a Windows or Android target. Platform-specific targets may require platform tooling and a supported operating system.

## Privacy notes

Local search should remain on-device. The app does not need a cloud database, account, analytics, or remote indexing. Any future online feature must be separate from private local search and must not upload filenames, paths, or document contents without explicit opt-in and a privacy review.

## License

License to be decided before public distribution.
