[CmdletBinding()]
param()
. "$PSScriptRoot/Team.Common.ps1"
$msbuild=Find-TeamMSBuild
& $msbuild (Join-Path $script:TeamRoot 'ChuuongTrinhQuanLyKhachSan.sln') /t:Build /p:Configuration=Release '/p:Platform=Any CPU' /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$testDirectory=Join-Path $env:TEMP ('nt106-operations-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
Copy-Item -LiteralPath (Join-Path $script:TeamRoot 'HotelServer/bin/Release/HotelServer.exe') -Destination $testDirectory
$compiler=Join-Path (Split-Path $msbuild -Parent) 'Roslyn/csc.exe'
$testExe=Join-Path $testDirectory 'HotelOperationsChecks.exe'
& $compiler /nologo /target:exe "/out:$testExe" /r:System.Data.dll /r:System.Configuration.dll /r:System.Web.Extensions.dll `
    "/r:$(Join-Path $testDirectory 'HotelServer.exe')" `
    (Join-Path $script:TeamRoot 'tests/HotelOperationsChecks.cs') (Join-Path $script:TeamRoot 'Networking/NetworkClient.cs')
if($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
# A uniquely named temporary database on localhost; never uses Azure or app config.
& $testExe
if($LASTEXITCODE -ne 0) { throw 'Hotel business integration checks failed.' }
