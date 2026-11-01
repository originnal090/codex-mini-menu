param([string]$HostWindow)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework/WPF/WindowsBase.dll","/r:$framework/WPF/PresentationCore.dll","/r:$framework/WPF/PresentationFramework.dll","/r:$framework/WPF/UIAutomationClient.dll","/r:$framework/WPF/UIAutomationProvider.dll","/r:$framework/WPF/UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root/src" -Filter '*.cs').FullName
& "$framework/csc.exe" /nologo /target:exe /main:ContextIntegrationTests @refs "/out:$root\artifacts\ContextIntegrationTests.exe" @sources "$root\tests\ContextIntegrationTests.cs"
if($LASTEXITCODE){throw 'Context view tests compilation failed'}
if($HostWindow){& "$root\artifacts\ContextIntegrationTests.exe" --probe $HostWindow}else{& "$root\artifacts\ContextIntegrationTests.exe" "$root\artifacts\context-view"}
if($LASTEXITCODE){throw 'Context view tests failed'}
