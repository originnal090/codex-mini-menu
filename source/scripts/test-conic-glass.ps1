param([string]$SourceDirectory='',[string]$Label='current')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
New-Item -ItemType Directory -Path "$root/artifacts/conic-glass/$Label" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationClient.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
if(-not $SourceDirectory){$SourceDirectory=Join-Path $root 'src'}
$sources=(Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:exe /main:ConicGlassTests @refs "/out:$root\artifacts\ConicGlassTests.exe" @sources "$root\tests\ConicGlassTests.cs"
if($LASTEXITCODE){throw 'Conic/glass test compilation failed'}
& "$root\artifacts\ConicGlassTests.exe" "$root\artifacts\conic-glass\$Label"
if($LASTEXITCODE){throw 'Conic/glass tests failed'}
