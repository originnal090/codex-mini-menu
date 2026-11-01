$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs').FullName
& "$framework\csc.exe" /nologo /target:exe /main:StatsViewTests @refs "/out:$root\artifacts\StatsViewTests.exe" @sources "$root\tests\StatsViewTests.cs"
if($LASTEXITCODE){throw 'Statistics view compilation failed'}
& "$root\artifacts\StatsViewTests.exe" "$root\artifacts\stats-views"
if($LASTEXITCODE){throw 'Statistics view tests failed'}
