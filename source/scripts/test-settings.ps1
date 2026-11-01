$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs=@('/r:Microsoft.CSharp.dll','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs').FullName
& "$framework\csc.exe" /nologo /target:exe /main:SettingsTests @refs "/out:$root\artifacts\SettingsTests.exe" @sources "$root\tests\SettingsTests.cs"
if($LASTEXITCODE){throw 'Settings test compilation failed'}
& "$root\artifacts\SettingsTests.exe" "$root\artifacts\settings"
if($LASTEXITCODE){throw 'Settings tests failed'}
