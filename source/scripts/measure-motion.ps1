param([string]$Label='current',[ValidateSet('tilt','drag')][string]$Mode='tilt',[string]$SourceDirectory='',[switch]$EnforceBudget)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
New-Item -ItemType Directory -Path "$root/artifacts/motion-performance" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationClient.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
if(-not $SourceDirectory){$SourceDirectory=Join-Path $root 'src'}
$sources=(Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:exe /optimize+ /main:MotionPerformance @refs "/out:$root\artifacts\MotionPerformance.exe" @sources "$root\tests\MotionPerformance.cs"
if($LASTEXITCODE){throw 'Motion benchmark compilation failed'}
$arguments=@("$root\artifacts\motion-performance",$Label,$Mode);if($EnforceBudget){$arguments+='enforce'}
& "$root\artifacts\MotionPerformance.exe" @arguments
if($LASTEXITCODE){throw 'Motion benchmark failed its 90 updates/second budget'}
