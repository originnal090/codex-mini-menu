param([ValidateSet('Check','Build','Test','TestUI','Package')][string]$Action='Check')
$ErrorActionPreference='Stop'
$project=$PSScriptRoot
$source=Join-Path $project 'source'
$scripts=Join-Path $source 'scripts'
$artifacts=Join-Path $source 'artifacts'
$temporary=Join-Path $artifacts 'tmp'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw 'Install .NET Framework 4.8 on Windows before building.'}
foreach($folder in @((Join-Path $source 'bin'),$artifacts,$temporary)){
  New-Item -ItemType Directory -Path $folder -Force | Out-Null
}
$previousTemp=$env:TEMP
$previousTmp=$env:TMP
Push-Location -LiteralPath $project
try {
  $env:TEMP=$temporary
  $env:TMP=$temporary
  $manifest=Get-Content -LiteralPath (Join-Path $project 'plugin\.codex-plugin\plugin.json') -Raw -Encoding UTF8 | ConvertFrom-Json
  Write-Output "Project: $project"
  Write-Output "Version: $($manifest.version)"
  Write-Output "Compiler: $compiler"
  Write-Output "Build output: $(Join-Path $source 'bin')"
  Write-Output "Test output: $artifacts"
  if($Action -eq 'Check'){
    foreach($relative in @('src\TimerDesktop.cs','scripts\build.ps1','scripts\package.ps1','tests\CoreTests.cs')){
      if(-not (Test-Path -LiteralPath (Join-Path $source $relative))){throw "Missing source file: $relative"}
    }
    # Extracted releases can build without Git. Check the worktree when it exists.
    if(Test-Path -LiteralPath (Join-Path $project '.git')){
      if(-not (Get-Command git -ErrorAction SilentlyContinue)){throw 'Git is required for this checkout.'}
      & git -C $project rev-parse --show-toplevel --git-common-dir
      if($LASTEXITCODE){throw 'Git worktree paths are invalid.'}
      & git -C $project remote -v
      if($LASTEXITCODE){throw 'Cannot read Git remotes.'}
    }
  } else {
    & (Join-Path $scripts 'build.ps1')
    if($Action -in @('Test','TestUI','Package')){
      foreach($test in @('test-cycle.ps1','test-stats.ps1','test-context.ps1')){
        & (Join-Path $scripts $test)
      }
    }
    if($Action -eq 'TestUI'){
      foreach($test in @('test-host-selection.ps1','test-structure.ps1','test-settings.ps1','test-islands.ps1','test-work-rest.ps1','test-stats-view.ps1','test-context-view.ps1','test-silver-card.ps1','test-card-refinement.ps1','test-back.ps1','test-duration-float.ps1','test-motion-polish.ps1','test-conic-glass.ps1','test-window.ps1')){
        & (Join-Path $scripts $test)
      }
    }
    if($Action -eq 'Package'){
      & (Join-Path $scripts 'package.ps1')
    }
  }
  Write-Output "PASS $Action"
} finally {
  $env:TEMP=$previousTemp
  $env:TMP=$previousTmp
  Pop-Location
}
