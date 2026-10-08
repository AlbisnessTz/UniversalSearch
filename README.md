# UniversalSearch

**A lightweight, privacy-first search app for Windows and Android.**

UniversalSearch is being developed as a local-first utility to help people find files, folders, applications, and text inside supported documents from one search interface. Local search should work without an account or internet connection; online search will remain optional.

## Current development status

This repository is at the MVP foundation stage. The first milestone establishes a shared .NET MAUI interface and a small C# search service. Platform-specific storage access and deeper document indexing will be added incrementally.

## Product principles

- **Local-first:** never upload local files for indexing.
- **Lightweight:** bounded results, asynchronous scans, and cancellation.
- **Privacy:** explicit search locations and exclusions; no account required.
- **Cross-platform:** shared C# search logic with platform-specific file access.
- **Honest scope:** Android storage is permission-limited; support will follow Android's scoped-storage rules.

## MVP roadmap

1. Shared search UI and basic filename/path search.
2. Windows folder selection, app discovery, and better indexing.
3. Full-text indexing for supported TXT, PDF, and DOCX files.
4. Android file search using user-granted access and Android document providers.
5. Optional web search, kept separate from private local search.

## Build prerequisites

- A supported .NET SDK matching the target framework in `UniversalSearch.csproj`.
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

The MVP performs local filesystem searches only. It does not require an online account, cloud database, analytics, or remote indexing. Do not add cloud upload of filenames, paths, or document contents without a clear opt-in and privacy review.

## License

License to be decided before public distribution.