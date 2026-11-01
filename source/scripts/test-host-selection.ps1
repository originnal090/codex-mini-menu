$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
$folder=Join-Path $root 'artifacts\host-selection'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
& "$framework\csc.exe" /nologo /target:winexe /main:HostSelectionTests @refs "/out:$folder\Codex.exe" @sources "$root\tests\HostSelectionTests.cs"
if($LASTEXITCODE){throw 'Test compilation failed'}
$log=Join-Path $folder 'result.txt'
$process=Start-Process -FilePath "$folder\Codex.exe" -ArgumentList ('"'+$log+'"') -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(30000)){throw 'Host selection test timeout'}
Get-Content $log
if($process.ExitCode){throw 'Host selection tests failed'}
