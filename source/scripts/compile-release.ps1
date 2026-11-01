$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationClient.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
$project=Split-Path -Parent $root
$version=(Get-Content -LiteralPath (Join-Path $project 'plugin\.codex-plugin\plugin.json') -Raw -Encoding UTF8 | ConvertFrom-Json).version
if($version -notmatch '^\d+\.\d+\.\d+$'){throw 'Plugin version must use major.minor.patch for the Windows executable.'}
New-Item -ItemType Directory -Path "$root\bin","$root\artifacts" -Force | Out-Null
$versionSource=Join-Path $root 'artifacts\AssemblyVersion.cs'
$attributes=@"
using System.Reflection;
[assembly: AssemblyVersion("$version.0")]
[assembly: AssemblyFileVersion("$version.0")]
[assembly: AssemblyInformationalVersion("$version")]
"@
[IO.File]::WriteAllText($versionSource,$attributes,[Text.UTF8Encoding]::new($false))
& "$framework/csc.exe" /nologo /target:winexe /optimize+ @refs "/out:$root\bin\CodexRingTimer.exe" @sources $versionSource
if($LASTEXITCODE){throw 'Application compilation failed'}
