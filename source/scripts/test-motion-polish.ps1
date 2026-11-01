$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
New-Item -ItemType Directory -Path "$root/artifacts/motion-polish" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationClient.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:exe /main:MotionPolishTests @refs "/out:$root/artifacts/MotionPolishTests.exe" @sources "$root\tests\MotionPolishTests.cs"
if($LASTEXITCODE){throw 'Motion polish test compilation failed'}
& "$root/artifacts/MotionPolishTests.exe" "$root/artifacts/motion-polish"
if($LASTEXITCODE){throw 'Motion polish tests failed'}
