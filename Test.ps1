[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'Build.ps1')
$package = Join-Path $root 'build\VTMB Subtitle Pause Fix'
$installer = Join-Path $package 'Installer.exe'
$process = Start-Process -FilePath $installer -ArgumentList '--verify', '--quiet' -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) { throw "Recipe verification failed with exit code $($process.ExitCode)." }
$actual = @(Get-ChildItem $package -Force | ForEach-Object Name | Sort-Object)
if (Compare-Object @('Installer.exe', 'README.md') $actual) { throw 'Unexpected public package contents.' }
$forbidden = @(Get-ChildItem $root -Recurse -File | Where-Object {
    $_.FullName -notlike (Join-Path $root 'build\*') -and $_.Extension -in '.dll', '.bsp', '.mdl', '.vpk', '.ico'
})
if ($forbidden.Count) { throw "Forbidden binary source files: $($forbidden.FullName -join ', ')" }
'All synthetic PE patch tests passed.'
