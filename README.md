# UniversalSearch

**A lightweight, privacy-first search app for Windows and Android.**

UniversalSearch is being developed as a local-first utility to help people find files, folders, images by filename, Windows Start Menu app shortcuts, selected Windows Settings pages, and text inside a limited set of small text files. Local search is designed to work without an account or internet connection; online search will remain optional and separate.

## Current development status

This is an early MVP foundation, not a release-ready app. The shared .NET MAUI interface and local filesystem search are present. The last completed GitHub Actions run passed Windows x64 compilation, Android compilation, and the six existing local-search unit tests. Subsequent Android hardening changes are awaiting a fresh CI run. **Compilation and unit tests do not prove the app works correctly on a physical Windows PC or Android phone.** Android folder selection and document-provider search have been implemented, but still require real-device verification.

### Current scope and limitations

- File and folder names are searched inside a folder selected by the user.
- Image files (including JPG/JPEG, PNG, GIF, BMP, WebP, TIFF, HEIC and SVG) are currently found by **filename**, not by image contents or visual similarity.
- Optional full-text search is limited to TXT, MD, CSV and LOG files whose provider-reported size is at most 512 KiB. Android content reading is also capped, and providers may deny access or report incomplete metadata.
- Search results are bounded, directory enumeration is incremental, and the UI has a Cancel button for an active search.
- Windows app discovery searches Start Menu shortcut files; it is not a complete inventory of every installed app.
- Windows Settings search covers a curated list of common pages and aliases. It is not an exhaustive search of every Windows setting.
- Android folder access uses the system document picker and depends on the selected storage provider and granted permissions. Real-device behavior has not yet been verified.
- PDF and DOCX text extraction, image-content search, indexing/caching and web search are planned work, not implemented features.

## Product principles

- **Local-first:** never upload local files for indexing.
- **Lightweight:** bounded results, asynchronous scans, incremental enumeration, and cancellation.
- **Privacy:** user-selected locations; no account required.
- **Cross-platform:** shared C# logic with platform-specific file access.
- **Honest scope:** Android storage is permission-limited; support follows Android's scoped-storage rules.

## Verification

The test project in `tests/UniversalSearch.Tests` covers file-name matching, folder-name matching, optional text-content matching, result limits and pre-cancelled searches. Check the [GitHub Actions runs](https://github.com/AlbisnessTz/UniversalSearch/actions) for the latest build and test status. Android document-provider behavior currently has no automated platform test and must be tested on a real phone.

## Next milestones

1. Keep Windows x64 and Android CI builds green and review warnings.
2. Test folder selection, file/folder matches, image-filename matches, and text-file search on a real Android phone.
3. Test folder, image-filename, app-shortcut and Settings flows on Windows.
4. Expand automated tests, including inaccessible paths, large directories, and cancellation during a scan.
5. Add supported PDF/DOCX text extraction and tests.
6. Consider image-content search and optional web search as separate, opt-in features.

## Build prerequisites

- A supported .NET SDK matching the target framework in UniversalSearch.csproj.
- .NET MAUI workload and platform tooling for the platform you want to run.
- For Windows, a compatible Windows SDK and Visual Studio Build Tools/Visual Studio workload.

Check the environment with `dotnet --info` and `dotnet workload list`.

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
