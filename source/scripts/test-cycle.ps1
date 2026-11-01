$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& "$framework/csc.exe" /nologo /target:exe /r:System.Web.Extensions.dll "/out:$root\artifacts\CycleTests.exe" "$root\src\TimerEngine.cs" "$root\src\Stats.cs" "$root\tests\CycleTests.cs"
if($LASTEXITCODE){throw 'Cycle tests compilation failed'}
& "$root\artifacts\CycleTests.exe" "$root\artifacts\cycle-data"
if($LASTEXITCODE){throw 'Cycle tests failed'}
