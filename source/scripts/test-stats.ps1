$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& "$framework/csc.exe" /nologo /target:exe /r:System.Web.Extensions.dll "/out:$root\artifacts\StatsTests.exe" "$root\src\TimerEngine.cs" "$root\src\Stats.cs" "$root\tests\StatsTests.cs"
if($LASTEXITCODE){throw 'Statistics tests compilation failed'}
& "$root\artifacts\StatsTests.exe" "$root\artifacts\stats-data"
if($LASTEXITCODE){throw 'Statistics tests failed'}
