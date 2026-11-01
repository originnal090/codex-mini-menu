param([switch]$Capture)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
New-Item -ItemType Directory -Path "$root/artifacts" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationClient.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:exe /main:DurationFloatTests @refs "/out:$root\artifacts\DurationFloatTests.exe" @sources "$root\tests\DurationFloatTests.cs"
if($LASTEXITCODE){throw 'Duration float test compilation failed'}
$arguments=@("$root\artifacts\duration-float")
if($Capture){$arguments+='--capture'}
& "$root\artifacts\DurationFloatTests.exe" @arguments
if($LASTEXITCODE){throw 'Duration float tests failed'}
