$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& "$framework\csc.exe" /nologo /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll "/r:$framework\WPF\UIAutomationClient.dll" "/r:$framework\WPF\UIAutomationTypes.dll" "/r:$framework\WPF\WindowsBase.dll" "/out:$root\artifacts\Codex.exe" "$root\tests\WindowTests.cs"
if($LASTEXITCODE){throw 'Window fixture compilation failed'}
$fixtureRoot=Join-Path $root 'artifacts/window-app'
New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'bin') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'bin/CodexRingTimer.exe') -Destination (Join-Path $fixtureRoot 'bin/CodexRingTimer.exe') -Force
Copy-Item -LiteralPath (Join-Path (Split-Path $root -Parent) 'plugin/modules') -Destination $fixtureRoot -Recurse -Force
$application=Join-Path $fixtureRoot 'bin/CodexRingTimer.exe'
$fixture=Start-Process -FilePath "$root\artifacts\Codex.exe" -ArgumentList @(('"'+$application+'"'),('"'+"$root\artifacts\window-follow-after.txt"+'"')) -WindowStyle Hidden -PassThru
if(-not $fixture.WaitForExit(60000)){throw 'Window test timeout; inspect the fixture log before retrying'}
Get-Content -LiteralPath "$root\artifacts\window-follow-after.txt"
if($fixture.ExitCode){throw 'Window follow test failed'}
