[CmdletBinding()]
param()
. "$PSScriptRoot/Team.Common.ps1"
$msbuild = Find-TeamMSBuild
$compiler = Join-Path (Split-Path $msbuild -Parent) 'Roslyn/csc.exe'
$testDirectory = Join-Path $env:TEMP ('HotelDesk-supabase-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
$sources = @('Models','Infrastructure','Services') | ForEach-Object {
    Get-ChildItem -LiteralPath (Join-Path $script:TeamRoot $_) -Filter '*.cs' | ForEach-Object FullName
}
$exe = Join-Path $testDirectory 'SupabaseChecks.exe'
& $compiler /nologo /target:exe "/out:$exe" /r:System.Net.Http.dll /r:System.Configuration.dll /r:System.Web.Extensions.dll $sources (Join-Path $script:TeamRoot 'tests/SupabaseChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Supabase checks compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Supabase checks failed.' }
Write-Host 'Offline tests only: no cloud account, network requests, or database writes.'
