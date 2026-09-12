[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Protean', 'Subtitle')]
    [string]$Kind,

    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$fullPath = [IO.Path]::GetFullPath($Path)
$directory = Split-Path -Parent $fullPath
if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$bitmap = New-Object Drawing.Bitmap 64, 64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::FromArgb(18, 18, 20))

$border = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(120, 18, 22)), 3
$graphics.DrawEllipse($border, 3, 3, 57, 57)

if ($Kind -eq 'Protean') {
    $claw = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(210, 32, 44)), 6
    $claw.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $claw.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($claw, 19, 48, 36, 14)
    $graphics.DrawLine($claw, 30, 51, 45, 18)
    $graphics.DrawLine($claw, 41, 50, 52, 24)
    $claw.Dispose()
}
else {
    $outline = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(225, 225, 214)), 4
    $graphics.DrawRectangle($outline, 12, 14, 40, 31)
    $graphics.DrawLine($outline, 20, 45, 16, 53)
    $graphics.DrawLine($outline, 20, 45, 29, 45)
    $pause = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(210, 32, 44)), 6
    $graphics.DrawLine($pause, 29, 22, 29, 37)
    $graphics.DrawLine($pause, 39, 22, 39, 37)
    $pause.Dispose()
    $outline.Dispose()
}

$handle = $bitmap.GetHicon()
$icon = [Drawing.Icon]::FromHandle($handle)
$stream = [IO.File]::Open($fullPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $icon.Save($stream) }
finally {
    $stream.Dispose()
    $icon.Dispose()
    $border.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
