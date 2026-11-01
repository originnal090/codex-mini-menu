$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Path (Join-Path $root 'bin') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root 'artifacts') -Force | Out-Null
& $compiler /nologo /target:exe /optimize+ /r:System.Web.Extensions.dll "/out:$root\artifacts\CoreTests.exe" "$root\src\TimerEngine.cs" "$root\tests\CoreTests.cs"
if ($LASTEXITCODE) { throw 'Test compilation failed' }
& "$root\artifacts\CoreTests.exe"
if ($LASTEXITCODE) { throw 'Core tests failed' }
$framework=Split-Path -Parent $compiler
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
& (Join-Path $PSScriptRoot 'compile-release.ps1')
