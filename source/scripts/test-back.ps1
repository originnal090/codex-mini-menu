$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:exe /main:BackAnchorTests @refs "/out:$root\artifacts\BackAnchorTests.exe" @sources "$root\tests\BackAnchorTests.cs"
if($LASTEXITCODE){throw 'Return tests compilation failed'}
& "$root\artifacts\BackAnchorTests.exe" "$root\artifacts\back-anchor"
if($LASTEXITCODE){throw 'Return anchor tests failed'}
