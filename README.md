# MP3 to M4B Audiobook Maker

A simple Windows GUI tool that combines a folder of MP3 files into a single M4B audiobook with chapter markers, cover art, and metadata.

No installation required. ffmpeg is downloaded automatically on first use.

## Features

- Combines any number of MP3s into a single `.m4b` file
- Chapter markers per track (named from the filename, track numbers stripped)
- Embeds cover art from the source MP3s
- Embeds title, author, and genre metadata
- Sort tracks by filename or modified date, or reorder manually
- Bitrate selection (32k–128k)
- ffmpeg downloaded automatically on first run (~60 MB, stored once in `%AppData%`)

## Requirements

- Windows 10 or 11
- Nothing else — no Python, no installs

## Usage

1. Download or clone this repo
2. Double-click **`AudiobookMaker.cmd`**
3. Browse to a folder of MP3 files
4. Fill in the book title and author
5. Click **Convert to M4B**

On first conversion, the app will offer to download ffmpeg automatically if it isn't already on your system.

If you prefer to supply ffmpeg yourself, place `ffmpeg.exe` in the same folder as `AudiobookMaker.vbs` and it will be used automatically.

## How it works

The tool uses [ffmpeg](https://ffmpeg.org) under the hood:

1. Reads the duration of each MP3 to calculate chapter timestamps
2. Extracts cover art from the first MP3
3. Concatenates and re-encodes the audio to AAC
4. Writes an ffmpeg metadata file containing chapter markers and book metadata
5. Muxes everything (audio + chapters + cover) into a single `.m4b`

## File structure

```
AudiobookMaker.cmd   Launch this to open the app
AudiobookMaker.ps1   The application (PowerShell + WinForms)
```

## License

MIT
