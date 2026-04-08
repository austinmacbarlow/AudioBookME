# Audiobook Maker

A Windows GUI app for combining audio files into M4B audiobooks with chapter markers, cover art, and metadata.

No installation required. ffmpeg is downloaded automatically on first use.

![screenshot placeholder](https://placeholder)

## Features

- Combine MP3, M4A, M4B, FLAC, OGG, OPUS, AAC, or WMA files into a single `.m4b`
- Chapter markers generated per file, with editable names before converting
- Embedded cover art — extracted from source files or choose your own image
- Title, author, series, and genre metadata
- Progress bar with estimated time remaining during encoding
- **Stream copy mode** — instant output with no quality loss for AAC sources
- Drag and drop folders or individual audio files onto the window
- Sort files by filename or modified date, or reorder manually
- Fix chapter metadata on an existing M4B
- Split an M4B into individual chapter files
- Remembers last bitrate setting across sessions
- ffmpeg downloaded automatically on first run (~60 MB, stored in `%AppData%\AudiobookMaker`)

## Requirements

- Windows 10 or 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — Windows prompts to install automatically if missing

## Usage

1. Run `AudiobookMaker.exe` from the `publish\` folder (build it first — see below)
2. Browse to a folder of audio files, or drag and drop files/folders onto the window
3. Fill in the book title and author
4. Optionally edit chapter names in the file list
5. Click **Convert to M4B**

## Building

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
cd C:\path\to\audioprocessor
.\build.ps1
```

Output: `publish\AudiobookMaker.exe`

For a self-contained exe that needs no runtime:
```powershell
dotnet publish AudiobookMaker.csproj -c Release -o publish --self-contained true -r win-x64
```

## How it works

Uses [ffmpeg](https://ffmpeg.org) under the hood:

1. Reads each file's duration to build chapter timestamps
2. Extracts cover art from the first source file (or uses the one you selected)
3. Concatenates and re-encodes audio to AAC at the selected bitrate (or stream-copies if "Copy" is chosen)
4. Writes an ffmpeg metadata file with chapter markers and book info
5. Muxes audio + chapters + cover into a single `.m4b`

## License

MIT
