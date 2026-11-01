$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:winexe /main:WorkRestInteractionTests @refs "/out:$root\artifacts\WorkRestInteractionTests.exe" @sources "$root\tests\WorkRestInteractionTests.cs"
if($LASTEXITCODE){throw 'Interaction test compilation failed'}
$folder=Join-Path $root 'artifacts/work-rest-interaction'
$test=Start-Process -FilePath "$root\artifacts\WorkRestInteractionTests.exe" -ArgumentList ('"'+$folder+'"') -WindowStyle Hidden -PassThru
if(-not $test.WaitForExit(30000)){throw 'Interaction tests still running; inspect result file'}
Get-Content (Join-Path $folder 'result.txt')
if($test.ExitCode){throw 'Interaction tests failed'}
