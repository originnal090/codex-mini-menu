$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
# The project root holds plugin/, .agents/ and the installers. When this script runs
# from the source/ folder of an extracted release, those live one level up.
$base=$root
if(-not (Test-Path -LiteralPath (Join-Path $root '.agents'))){$base=Split-Path -Parent $root}
Add-Type -AssemblyName System.Drawing
$assets=Join-Path $base 'plugin\assets'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
function New-MenuIconPng([string]$path,[int]$red,[int]$green,[int]$blue){
  $bitmap=New-Object Drawing.Bitmap 128,128
  $graphics=[Drawing.Graphics]::FromImage($bitmap)
  $graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $graphics.Clear([Drawing.Color]::Transparent)
  $color=[Drawing.Color]::FromArgb($red,$green,$blue)
  $pen=New-Object Drawing.Pen $color,8
  $pen.StartCap=[Drawing.Drawing2D.LineCap]::Round;$pen.EndCap=$pen.StartCap
  $graphics.DrawEllipse($pen,12,12,104,104)
  $dotBrush=New-Object Drawing.SolidBrush $color
  foreach($x in @(46,72)){foreach($y in @(46,72)){$graphics.FillEllipse($dotBrush,$x,$y,10,10)}}
  $dotBrush.Dispose();$pen.Dispose();$graphics.Dispose()
  $bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Png);$bitmap.Dispose()
}
# icon.png is the light mark used on dark themes; icon-dark.png is the dark mark
# for the light theme, where the light mark sat at about 1.8:1 contrast.
New-MenuIconPng (Join-Path $assets 'icon.png') 184 184 184
New-MenuIconPng (Join-Path $assets 'icon-dark.png') 46 46 46
$png=[IO.File]::ReadAllBytes((Join-Path $assets 'icon.png'))
$stream=[IO.File]::Create((Join-Path $assets 'icon.ico'))
$writer=[IO.BinaryWriter]::new($stream)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]1)
$writer.Write([byte]128);$writer.Write([byte]128);$writer.Write([byte]0);$writer.Write([byte]0)
$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$png.Length);$writer.Write([uint32]22);$writer.Write($png)
$writer.Dispose()
New-Item -ItemType Directory -Path (Join-Path $base 'plugin\bin') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'bin\CodexRingTimer.exe') -Destination (Join-Path $base 'plugin\bin\CodexRingTimer.exe') -Force
$stage=Join-Path $base 'dist\CodexMiniMenu'
if(Test-Path -LiteralPath $stage){if([IO.Path]::GetFullPath($stage) -ne (Join-Path ([IO.Path]::GetFullPath($base)) 'dist\CodexMiniMenu')){throw 'Unexpected staging path'};Remove-Item -LiteralPath $stage -Recurse -Force}
New-Item -ItemType Directory -Path $stage -Force | Out-Null
foreach($name in @('plugin','.agents','install.ps1','uninstall.ps1','install.cmd','uninstall.cmd','start.cmd','README.md','README.zh-CN.md','CONTEXT.md','docs')){Copy-Item -LiteralPath (Join-Path $base $name) -Destination $stage -Recurse -Force}
New-Item -ItemType Directory -Path (Join-Path $stage 'source') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'src') -Destination (Join-Path $stage 'source') -Recurse
Copy-Item -LiteralPath (Join-Path $root 'tests') -Destination (Join-Path $stage 'source') -Recurse
Copy-Item -LiteralPath (Join-Path $root 'scripts') -Destination (Join-Path $stage 'source') -Recurse
# Windows PowerShell 5.1 must decode Chinese literals consistently.
Get-ChildItem -LiteralPath $stage -Filter '*.ps1' -Recurse | ForEach-Object {$t=[IO.File]::ReadAllText($_.FullName);[IO.File]::WriteAllText($_.FullName,$t,[Text.UTF8Encoding]::new($true))}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$version=(Get-Content -LiteralPath (Join-Path $base 'plugin\.codex-plugin\plugin.json') -Raw -Encoding UTF8 | ConvertFrom-Json).version
$zip=Join-Path $base "dist\CodexMiniMenu-Windows-$version.zip"
if(Test-Path -LiteralPath $zip){Remove-Item -LiteralPath $zip}
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[IO.Compression.CompressionLevel]::Optimal,$true)
Get-Item -LiteralPath $zip | Select-Object FullName,Length
