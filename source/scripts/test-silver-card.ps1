$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Path "$root\artifacts\silver-card" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root\src" -Filter '*.cs').FullName
& "$framework\csc.exe" /nologo /target:exe /main:SilverCardTests @refs "/out:$root\artifacts\SilverCardTests.exe" @sources "$root\tests\SilverCardTests.cs"
if($LASTEXITCODE){throw 'Silver card test compilation failed'}
& "$root\artifacts\SilverCardTests.exe" "$root\artifacts\silver-card"
if($LASTEXITCODE){throw 'Silver card tests failed'}
