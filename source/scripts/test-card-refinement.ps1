$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Path "$root\artifacts\card-refinement" -Force | Out-Null
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',"/r:$framework\WPF\WindowsBase.dll","/r:$framework\WPF\PresentationCore.dll","/r:$framework\WPF\PresentationFramework.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationProvider.dll","/r:$framework\WPF\UIAutomationTypes.dll")
$sources=(Get-ChildItem -LiteralPath "$root\src" -Filter '*.cs').FullName
& "$framework\csc.exe" /nologo /target:exe /main:CardRefinementTests @refs "/out:$root\artifacts\CardRefinementTests.exe" @sources "$root\tests\CardRefinementTests.cs"
if($LASTEXITCODE){throw 'Card refinement test compilation failed'}
& "$root\artifacts\CardRefinementTests.exe" "$root\artifacts\card-refinement"
if($LASTEXITCODE){throw 'Card refinement tests failed'}
