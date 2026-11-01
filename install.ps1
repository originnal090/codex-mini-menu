param([switch]$NoLaunch)
$ErrorActionPreference='Stop'
$source=[IO.Path]::GetFullPath($PSScriptRoot)
$destination=Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex\tools\codex-ring-timer'
$exeRelative='plugin\bin\CodexRingTimer.exe'
if (-not (Test-Path -LiteralPath (Join-Path $source $exeRelative))) { throw 'Missing timer executable. Extract the entire ZIP before installing.' }
if ((Test-Path -LiteralPath $destination) -and -not (Test-Path -LiteralPath (Join-Path $destination 'ring-timer-install.json'))) { throw 'Destination exists without the timer installation marker.' }
if (Test-Path -LiteralPath (Join-Path $destination $exeRelative)) {
  Start-Process -FilePath (Join-Path $destination $exeRelative) -ArgumentList '--exit' -WindowStyle Hidden -Wait
  # Wait for the previous process to release its image before replacing files.
  $timerExitDeadline=[DateTime]::UtcNow.AddSeconds(10)
  do {
    try { $timerImage=[IO.File]::Open((Join-Path $destination $exeRelative),'Open','ReadWrite','None');$timerImage.Dispose();break }
    catch [IO.IOException] { if([DateTime]::UtcNow -ge $timerExitDeadline){throw 'The menu has not exited yet. Close it from its tray menu and run the installer again.'};Start-Sleep -Milliseconds 100 }
  } while($true)
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
if ($source -ne [IO.Path]::GetFullPath($destination)) {
  foreach ($name in @('plugin','.agents','install.ps1','uninstall.ps1','install.cmd','uninstall.cmd','start.cmd','README.md')) {
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination $destination -Recurse -Force
  }
}
$exe=Join-Path $destination $exeRelative
$wsh=New-Object -ComObject WScript.Shell
$shortcuts=@()
foreach ($folder in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))) {
  $path=Join-Path $folder 'Codex Mini Menu.lnk'
  $link=$wsh.CreateShortcut($path)
  if ((Test-Path -LiteralPath $path) -and $link.TargetPath -ne $exe) {
    $previousRoot=Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $link.TargetPath))
    $previousMarker=Join-Path $previousRoot 'ring-timer-install.json'
    if(-not (Test-Path -LiteralPath $previousMarker) -or (Get-Content -LiteralPath $previousMarker -Raw | ConvertFrom-Json).name -ne 'Codex Ring Timer Lite'){throw "Shortcut belongs to another application: $path"}
  }
  $link.TargetPath=$exe;$link.Arguments='--open';$link.WorkingDirectory=(Split-Path -Parent $exe);$link.IconLocation=(Join-Path $destination 'plugin\assets\icon.ico');$link.Description='Codex Mini Menu';$link.Save();$shortcuts+=$path
  $legacy=Join-Path $folder 'Codex Ring Timer.lnk'
  if(Test-Path -LiteralPath $legacy){$oldLink=$wsh.CreateShortcut($legacy);if($oldLink.TargetPath -eq $exe){Remove-Item -LiteralPath $legacy}}
}
$codex=Get-Command codex -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
if (-not $codex) {
  $package=Get-AppxPackage -Name OpenAI.Codex -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($package) { $candidate=Join-Path $package.InstallLocation 'app\resources\codex.exe'; if(Test-Path -LiteralPath $candidate){$codex=$candidate} }
}
$registered=$false
if ($codex) {
  & $codex plugin marketplace add $destination
  if ($LASTEXITCODE -eq 0) {
    & $codex plugin add 'codex-ring-timer@ring-timer-local'
    $registered=($LASTEXITCODE -eq 0)
  }
}
$version=(Get-Content -LiteralPath (Join-Path $destination 'plugin\.codex-plugin\plugin.json') -Raw | ConvertFrom-Json).version
$marker=[ordered]@{name='Codex Ring Timer Lite';displayName='Codex Mini Menu';version=$version;directory=$destination;shortcuts=$shortcuts;pluginRegistered=$registered;codexCommand=$codex;modules=@('timer')}
$marker | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $destination 'ring-timer-install.json') -Encoding UTF8
if (-not $NoLaunch){Start-Process -FilePath $exe -WindowStyle Hidden}
Write-Output "Installed: $destination"
Write-Output "Codex plugin registered: $registered"
if (-not $registered) {Write-Warning 'The timer is ready. To register the Codex entry, use a Codex version with local marketplace support and run install.cmd again.'}
