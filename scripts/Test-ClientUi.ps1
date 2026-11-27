[CmdletBinding()]
param([string]$OutputDirectory)
. "$PSScriptRoot/Team.Common.ps1"
if([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory=Join-Path $env:TEMP ('NT106-ui-'+[guid]::NewGuid().ToString('N'))
}
$msbuild=Find-TeamMSBuild
& $msbuild (Join-Path $script:TeamRoot 'ChuuongTrinhQuanLyKhachSan.csproj') /t:Build /p:Configuration=Release /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0) { throw 'Client build failed.' }
$runtime=Join-Path $script:TeamRoot 'bin/Release'
$testDirectory=Join-Path $env:TEMP ('NT106-ui-check-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
foreach($file in @('ChuuongTrinhQuanLyKhachSan.exe','Guna.UI2.dll')) {
    Copy-Item -LiteralPath (Join-Path $runtime $file) -Destination $testDirectory
}
$compiler=Join-Path (Split-Path $msbuild -Parent) 'Roslyn/csc.exe'
$exe=Join-Path $testDirectory 'UiLayoutChecks.exe'
& $compiler /nologo /target:exe "/out:$exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll `
    "/r:$(Join-Path $testDirectory 'ChuuongTrinhQuanLyKhachSan.exe')" (Join-Path $script:TeamRoot 'tests/UiLayoutChecks.cs')
if($LASTEXITCODE -ne 0) { throw 'UI checks compilation failed.' }
& $exe $OutputDirectory
if($LASTEXITCODE -ne 0) { throw 'UI layout checks failed.' }
