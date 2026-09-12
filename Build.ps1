[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$buildRoot = Join-Path $sourceRoot 'build'
$packageName = 'VTMB Subtitle Pause Fix'
$outputRoot = Join-Path $buildRoot $packageName
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$iconPath = Join-Path $buildRoot '.installer.ico'
$requiredFiles = @('Installer.cs', 'PatchEngine.cs', 'PatchData.cs', 'manifest.json', 'app.manifest', 'New-InstallerIcon.ps1', 'ReleaseREADME.md')

foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $relativePath) -PathType Leaf)) {
        throw "Required build input is missing: $relativePath"
    }
}
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw '.NET Framework C# compiler was not found.'
}

$resolvedSource = [IO.Path]::GetFullPath($sourceRoot).TrimEnd('\')
$resolvedBuild = [IO.Path]::GetFullPath($buildRoot).TrimEnd('\')
if (-not $resolvedBuild.StartsWith($resolvedSource + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Unsafe build directory.'
}
if (Test-Path -LiteralPath $resolvedBuild) {
    Remove-Item -LiteralPath $resolvedBuild -Recurse -Force
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
& (Join-Path $sourceRoot 'New-InstallerIcon.ps1') -Kind Subtitle -Path $iconPath

$arguments = @(
    '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/warn:4',
    '/define:SUBTITLE',
    ('/out:' + (Join-Path $outputRoot 'Installer.exe')),
    ('/win32icon:' + $iconPath),
    ('/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')),
    ('/resource:' + (Join-Path $sourceRoot 'manifest.json') + ',VTMB.Manifest.json'),
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll',
    (Join-Path $sourceRoot 'Installer.cs'),
    (Join-Path $sourceRoot 'PatchEngine.cs'),
    (Join-Path $sourceRoot 'PatchData.cs')
)

& $compiler @arguments
if ($LASTEXITCODE -ne 0) {
    throw 'C# compilation failed.'
}

Copy-Item -LiteralPath (Join-Path $sourceRoot 'ReleaseREADME.md') -Destination (Join-Path $outputRoot 'README.md')
Remove-Item -LiteralPath $iconPath -Force

Write-Host "Patch-only package build complete: $outputRoot"
