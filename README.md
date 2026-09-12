# VTMB Subtitle Pause Fix

Fixes spoken-subtitle timing while *Vampire: The Masquerade - Bloodlines* is paused. Radio, television and cinematic subtitles resume from the correct point after unpausing instead of skipping the paused interval.

## Patch-only distribution

This repository contains no complete game DLL. The Installer verifies `Bin\engine.dll` from the user's local installation and builds the patched DLL in memory from the embedded changed-byte recipe.

## Requirements

- Windows
- Steam version of Vampire: The Masquerade - Bloodlines, Vanilla or Unofficial Patch
- Supported original `Bin\engine.dll`

## Building

Visual Studio and third-party modules are not required. See [BUILDING.md](BUILDING.md) for the Windows PowerShell 5.1 and PowerShell 7 commands.

The public build contains only `Installer.exe` and `README.md`.

## Source layout

- `Installer.cs` — WinForms UI, transactions, Backup and CLI
- `PatchEngine.cs` — checked in-memory patch application
- `PatchData.cs` — generated changed-byte recipe, not a complete game DLL
- `manifest.json` — supported hashes and recipe identifier
- `New-InstallerIcon.ps1` — original icon generator
- `Build.ps1` — autonomous release build
- `Test.ps1` — build, package checks and synthetic PE test

## License and disclosure

Original source code, recipes, scripts and documentation are available under the MIT License. Game names and trademarks belong to their respective owners; see [NOTICE](NOTICE).

Generative AI was used during development of code, UI text and promotional media. The complete reviewable source is published here.
