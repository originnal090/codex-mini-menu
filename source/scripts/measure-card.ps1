param([string]$Label='current',[string]$SourceDirectory='')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Path "$root\artifacts\card-performance" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
if(-not $SourceDirectory){$SourceDirectory=Join-Path $root 'src'}
$sources=(Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.cs').FullName
& "$framework\csc.exe" /nologo /target:exe /main:CardPerformance @refs "/out:$root\artifacts\CardPerformance.exe" @sources "$root\tests\CardPerformance.cs"
if($LASTEXITCODE){throw 'Performance fixture compilation failed'}
& "$root\artifacts\CardPerformance.exe" "$root\artifacts\card-performance" $Label
if($LASTEXITCODE){throw 'Performance fixture failed'}
