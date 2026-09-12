# Building VTMB Subtitle Pause Fix

## Requirements

- Windows 10 or later
- .NET Framework 4.x with `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`
- Windows PowerShell 5.1 or PowerShell 7

No Visual Studio installation or external PowerShell module is required.

## Build

From the repository root, run either:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

or:

```powershell
pwsh -NoProfile -File .\Build.ps1
```

The script creates exactly:

```text
build\VTMB Subtitle Pause Fix\
  Installer.exe
  README.md
```

The manifest and patch recipe are embedded in `Installer.exe`. The executable is unsigned and requires no PowerShell installation at runtime.

## Verify

```powershell
$installer = '.\build\VTMB Subtitle Pause Fix\Installer.exe'
$process = Start-Process -FilePath $installer -ArgumentList '--verify', '--quiet' -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) { throw 'Package verification failed.' }
```

Verification checks the embedded recipe structure and registry without requiring game files. Install-time checks additionally validate the local source DLL and generated SHA-256 before writing.
