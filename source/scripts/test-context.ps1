$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$files=@("$root\tests\ContextTests.cs")
if(Test-Path "$root\src\ContextUsage.cs"){$files+="$root\src\ContextUsage.cs"}
& "$framework/csc.exe" /nologo /target:exe /r:System.Web.Extensions.dll "/out:$root\artifacts\ContextTests.exe" @files
if($LASTEXITCODE){throw 'Context tests compilation failed'}
& "$root\artifacts\ContextTests.exe"
if($LASTEXITCODE){throw 'Context tests failed'}

