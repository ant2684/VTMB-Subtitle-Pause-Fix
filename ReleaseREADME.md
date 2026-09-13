# VTMB Subtitle Pause Fix

Version 1.0.0

Pauses spoken-subtitle timing while the game is paused. After unpausing, radio, television and cinematic subtitles continue from the same point instead of skipping the paused interval.

## Requirements

- Windows
- Steam version of Vampire: The Masquerade - Bloodlines, Vanilla or Unofficial Patch
- Original `Bin\engine.dll` with SHA-256 `9D00B2C1E5EDBAD052FB0B514634BA54574666E12D1B408C46CE141D51ED2313`

## Installation

1. Extract both files to a writable folder and close the game.
2. Run `Installer.exe` and select the folder containing `Vampire.exe`.
3. Keep **Create persistent backup** enabled and click **Install**.

Use **Restore** to return `Bin\engine.dll` exactly to its pre-package state. Keep the Installer folder and its `Backup` folder while Restore may be needed.

The patch-only Installer contains no complete game DLL. It validates the local original, builds the fix locally, supports read-only files and rolls back interrupted operations. Unsupported DLLs are never overwritten.

## Compatibility

Protean Mod Pack does not modify `engine.dll`, so the two packages can be installed and restored independently. Other mods replacing `engine.dll` require an explicit binary merge.
