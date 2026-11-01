$ErrorActionPreference='Stop'
$expected=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex\tools\codex-ring-timer'))
$target=[IO.Path]::GetFullPath($PSScriptRoot)
if ($target -ne $expected) { throw 'Run the uninstall script from the installed application folder.' }
$marker=Get-Content -LiteralPath (Join-Path $target 'ring-timer-install.json') -Raw | ConvertFrom-Json
if ($marker.name -ne 'Codex Ring Timer Lite' -or [IO.Path]::GetFullPath($marker.directory) -ne $target) { throw 'Invalid installation marker' }
$exe=Join-Path $target 'plugin\bin\CodexRingTimer.exe'
Start-Process -FilePath $exe -ArgumentList '--exit' -WindowStyle Hidden -Wait
Start-Sleep -Milliseconds 700
if ($marker.pluginRegistered -and (Test-Path -LiteralPath $marker.codexCommand)) {
  & $marker.codexCommand plugin remove 'codex-ring-timer@ring-timer-local'
  if ($LASTEXITCODE) {throw 'Codex plugin removal failed; application files were kept.'}
  & $marker.codexCommand plugin marketplace remove 'ring-timer-local'
}
$wsh=New-Object -ComObject WScript.Shell
foreach($path in $marker.shortcuts){if(Test-Path -LiteralPath $path){$link=$wsh.CreateShortcut($path);if($link.TargetPath -eq $exe){Remove-Item -LiteralPath $path}}}
$startup=Join-Path ([Environment]::GetFolderPath('Startup')) 'CodexRingTimerLite.lnk'
if(Test-Path -LiteralPath $startup){$link=$wsh.CreateShortcut($startup);if([IO.Path]::GetFileName($link.TargetPath) -eq 'CodexRingTimer.exe'){Remove-Item -LiteralPath $startup}}
# Canonical absolute target and marker were verified above. Preserve sibling state.json.
Remove-Item -LiteralPath $target -Recurse -Force
Write-Output 'Timer removed. Timer preferences and official Codex retained.'
