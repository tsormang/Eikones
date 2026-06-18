# Eikones

**Eikones** (Greek: εικόνες — *images*) is a lightweight Windows desktop app for quickly browsing, previewing, and organizing large photo collections. It is built for folders with hundreds or thousands of images, including large JPEG, PNG, and other common formats.

## What it does

Eikones helps you triage images from a **source folder** into a **destination folder** without opening a separate file manager or photo viewer for every file. You scroll through thumbnails, inspect each image at full quality, then delete or move it with a single keystroke.

The main window is split into three columns:

| Column | Purpose |
|--------|---------|
| **Source** | Virtualized thumbnail grid of images in your source folder |
| **Preview** | Large preview with progressive loading (blurred thumbnail first, then full resolution) |
| **Destination** | List of files already moved to the destination folder |

## Features

- **Fast browsing** — UI virtualization keeps memory use low even with 1,000+ images
- **Progressive preview** — Shows a low-res thumbnail immediately, then swaps in a higher-resolution preview in the background
- **EXIF metadata** — Displays date taken when available
- **Move & delete** — Move selected images to the destination folder, or send them to the Recycle Bin
- **Restore** — Right-click a file in the destination list to restore it to its original source folder
- **Persistent settings** — Source/destination paths and window size/position are saved between sessions
- **Move history** — Tracks where files came from so restores go back to the right place

## Keyboard shortcuts

| Key | Action |
|-----|--------|
| `Delete` | Delete the current image (Recycle Bin) |
| `Enter` or `M` | Move the current image to the destination folder |
| `Ctrl+,` | Open settings |

## Supported formats

JPEG, PNG, WebP, GIF, BMP, and TIFF (`.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, `.bmp`, `.tif`, `.tiff`).

## Getting started

### Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and run

```bash
dotnet build
dotnet run
```

On first launch, open **Settings** (`Ctrl+,`) and set your source and destination folders.

## Architecture

Eikones is a **WPF** app targeting **.NET 8**, structured with **MVVM** (Model–View–ViewModel):

- **ViewModels** — UI state and commands (`MainViewModel`, `PreviewViewModel`, etc.)
- **Services** — File scanning, thumbnails, preview loading, transfers, and settings persistence
- **Views** — XAML user controls for each column

Heavy work (disk I/O, image decoding, file moves) runs off the UI thread so scrolling and selection stay responsive.

For detailed performance and implementation notes, see [`Init.md`](Init.md).
